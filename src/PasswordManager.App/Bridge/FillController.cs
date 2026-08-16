using System.Windows;
using PasswordManager.App.Dialogs;
using PasswordManager.Models;

namespace PasswordManager.App.Bridge;

/// <summary>
/// يحلّ طلبات التعبئة القادمة من المتصفح: يبحث عن الحسابات المطابقة
/// ويعرض نافذة اختيار يقرر فيها المستخدم ماذا يُعبأ وأي رمز.
/// </summary>
internal sealed class FillController
{
    private readonly Func<VaultSession?> _getSession;
    private readonly Func<Window?> _getOwner;

    public FillController(Func<VaultSession?> getSession, Func<Window?> getOwner)
    {
        _getSession = getSession;
        _getOwner = getOwner;
    }

    public FillDecision? Resolve(FillRequest request)
    {
        var session = _getSession();
        if (session is null)
            return new FillDecision { Decision = "locked" };

        var host = HostOfUrl(request.Url);
        var title = request.Title ?? string.Empty;

        var matches = session.Data.Passwords
            .Where(p => Matches(p, host, title))
            .ToList();

        if (matches.Count == 0)
            return new FillDecision { Decision = "none" };

        var owner = _getOwner();
        if (owner is MainWindow { WindowState: WindowState.Minimized } main)
            main.WindowState = WindowState.Normal;

        var window = new FillConfirmWindow
        {
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        window.Setup(
            typingMode: false,
            entries: matches,
            allTotp: session.Data.TotpAccounts,
            siteUrl: request.Url,
            siteTitle: title);

        window.ShowDialog();
        return window.Result ?? new FillDecision { Decision = "none" };
    }

    private static bool Matches(PasswordEntry entry, string host, string title)
    {
        if (string.IsNullOrEmpty(host)) return false;

        var website = (entry.Website ?? string.Empty).Trim();
        if (website.Length > 0)
        {
            if (website.IndexOf(host, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            var entryHost = HostOfUrl(website);
            if (entryHost.Length > 0 &&
                (string.Equals(host, entryHost, StringComparison.OrdinalIgnoreCase) ||
                 host.EndsWith("." + entryHost, StringComparison.OrdinalIgnoreCase) ||
                 entryHost.EndsWith("." + host, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        if (!string.IsNullOrEmpty(title))
        {
            var entryTitle = (entry.Title ?? string.Empty).Trim();
            if (entryTitle.Length > 0 &&
                (entryTitle.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 title.IndexOf(entryTitle, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }
        }

        return false;
    }

    public static string HostOfUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
            return uri.Host.ToLowerInvariant().TrimStart("www.".ToCharArray());
        return url.ToLowerInvariant().TrimStart("www.".ToCharArray());
    }
}
