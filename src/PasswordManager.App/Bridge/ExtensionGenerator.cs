using System.IO;
using System.Text;

namespace PasswordManager.App.Bridge;

/// <summary>
/// توليد ملفات امتداد المتصفح (MV3) داخل مجلد التطبيق.
/// الهوية ثابتة لأن ملفات الامتداد تُوقَّع بمفتاح RSA مخزن في أصول التطبيق.
/// </summary>
internal static class ExtensionGenerator
{
    /// <summary>مفتاح التوقيع العام (SPKI) بصيغة Base64 — مشتق منه معرّف الامتداد الثابت.</summary>
    private const string PublicKeyBase64 =
        "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAtkLgkgx7Q8o5F3tPTUy3h9gjxmys+hl/hmvzGIm10iNENULVI8XPAehAf4XS1t3vpbiYmxBuBEcMAal7UdReztFkXMEvwIag/W8Ii/b8cc05G+o62eS6cOGVnrNPCCyaXzR2nrX8T4rYE+Gj3iq875zULJ3A6VUd8IHr0zGpj+j9INT5zVKSXYazdLcowjgzMaFmN/fDjauzcTGqwf0F+CIqsPExCmnXkZUlGy0Kv7eWCTiq3DklLrrtOr9lN3n4ELwqMzgMyEuknMu5JVxtJqxEWuVm4or4tr0lcKbYz8KNJnlOqZrnCFi/9QA8zJAezlL4U9WiSq0GPiidgyGMKwIDAQAB";

    private const string ManifestJson = """
        {
          "manifest_version": 3,
          "name": "مدير كلمات المرور — جسر المتصفح",
          "short_name": "PASMAN Bridge",
          "version": "1.0.0",
          "description": "جسر التعبئة التلقائية لمدير كلمات المرور. عند طلب الموقع كلمة مرور يُعرض عليك اختيار الحساب والرمز.",
          "key": "{KEY}",
          "permissions": ["nativeMessaging"],
          "background": { "service_worker": "background.js" },
          "content_scripts": [
            { "matches": ["<all_urls>"], "js": ["content.js"], "run_at": "document_idle", "all_frames": true }
          ],
          "action": { "default_title": "Password Manager" },
          "host_permissions": ["<all_urls>"]
        }
        """;

    private const string BackgroundJs = """
        const BROWSER_ID = "pasman";

        chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
          if (!msg || msg.type !== "requestFill") return;
          const tabId = sender.tab ? sender.tab.id : null;
          let done = false;

          const finish = (res) => {
            if (done) return;
            done = true;
            sendResponse(res);
            try { port.disconnect(); } catch (e) {}
          };

          const port = chrome.runtime.connectNative("com.pasman.bridge");
          port.onMessage.addListener((res) => {
            if (!res || res.type !== "fillResponse") return;
            finish(res);
            if (tabId !== null && res.decision === "fill") {
              chrome.tabs.sendMessage(tabId, {
                type: "fillResult",
                username: res.username || "",
                password: res.password || "",
                totp: res.totp || "",
                accountName: res.totpAccountName || "",
                entryTitle: res.entryTitle || ""
              }).catch(() => {});
            }
          });
          port.onDisconnect.addListener(() => {
            finish({ type: "fillResponse", decision: "notrunning", username: "", password: "", totp: "" });
          });
          port.postMessage({ type: "fillRequest", browser: BROWSER_ID, url: msg.url || "", title: msg.title || "" });
          return true;
        });

        chrome.action.onClicked.addListener((tab) => {
          if (!tab || !tab.id) return;
          if (isChromePage(tab.url || "")) return;
          chrome.tabs.sendMessage(tab.id, { type: "requestFill" }).catch(() => {});
        });

        function isChromePage(url) {
          return /^chrome:/.test(url) || /^edge:/.test(url) || /^brave:/.test(url) ||
                 /^about:/.test(url) || /^opera:/.test(url) || /^vivaldi:/.test(url) ||
                 /^chrome-extension:/.test(url);
        }
        """;

    private const string ContentJs = """
        (() => {
          function isChromePage() {
            const u = location.href;
            return /^chrome:/.test(u) || /^edge:/.test(u) || /^brave:/.test(u) ||
                   /^about:/.test(u) || /^opera:/.test(u) || /^vivaldi:/.test(u) ||
                   /^chrome-extension:/.test(u);
          }

          let timer = null;
          let lastUrl = location.href;

          function requestFill() {
            if (isChromePage()) return;
            if (location.href !== lastUrl) lastUrl = location.href;
            chrome.runtime.sendMessage(
              { type: "requestFill", url: location.href, title: document.title },
              () => {}
            );
          }

          function scheduleRequest() {
            if (timer) clearTimeout(timer);
            timer = setTimeout(() => {
              if (document.hasFocus()) requestFill();
            }, 350);
          }

          document.addEventListener("focusin", (e) => {
            const t = e.target;
            if (!t || !t.tagName || t.tagName.toLowerCase() !== "input") return;
            if (t.type === "password") scheduleRequest();
          }, true);

          document.addEventListener("click", (e) => {
            const t = e.target;
            if (!t || !t.tagName) return;
            const el = t.closest ? t.closest('input[type="password"]') : null;
            if (el) scheduleRequest();
          }, true);

          chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
            if (msg && msg.type === "requestFill") requestFill();
            if (msg && msg.type === "ping") sendResponse({ pong: true });
          });

          chrome.runtime.onMessage.addListener((msg) => {
            if (!msg || msg.type !== "fillResult") return;
            fillResult(msg);
          });

          function fillResult(res) {
            const pw = findPasswordField();
            if (!pw) return;
            if (res.username) setValue(findUsernameField(pw), res.username);
            if (res.password) setValue(pw, res.password);
            if (res.totp) setValue(findTotpField() || findUsernameField(pw), res.totp);
          }

          function findPasswordField() {
            return document.querySelector('input[type="password"]');
          }

          function findUsernameField(pw) {
            const scope = (pw && pw.closest("form")) || document;
            const list = scope.querySelectorAll(
              'input[type="text"], input[type="email"], input:not([type])'
            );
            for (const el of list) {
              if (el.disabled || el.readOnly) continue;
              if (!el.value) return el;
            }
            return null;
          }

          function findTotpField() {
            const sel = 'input[maxlength="6"], input[maxlength="7"], input[maxlength="8"], ' +
                        'input[name*="otp" i], input[name*="totp" i], input[name*="2fa" i], ' +
                        'input[name*="code" i], input[autocomplete="one-time-code"]';
            const el = document.querySelector(sel);
            if (el && !el.value && !el.disabled) return el;
            return null;
          }

          function setValue(el, value) {
            if (!el || value === undefined || value === null) return;
            const proto = el instanceof HTMLTextAreaElement
              ? HTMLTextAreaElement.prototype
              : HTMLInputElement.prototype;
            const setter = Object.getOwnPropertyDescriptor(proto, "value").set;
            setter.call(el, String(value));
            el.dispatchEvent(new Event("input", { bubbles: true }));
            el.dispatchEvent(new Event("change", { bubbles: true }));
          }
        })();
        """;

    /// <summary>
    /// يولد ملفات الامتداد (idempotent). يعيد true عند النجاح.
    /// </summary>
    public static bool Generate()
    {
        try
        {
            Directory.CreateDirectory(BridgeConstants.ExtensionDir);
            File.WriteAllText(
                Path.Combine(BridgeConstants.ExtensionDir, "manifest.json"),
                ManifestJson.Replace("{KEY}", PublicKeyBase64),
                new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(BridgeConstants.ExtensionDir, "background.js"), BackgroundJs, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(BridgeConstants.ExtensionDir, "content.js"), ContentJs, new UTF8Encoding(false));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
