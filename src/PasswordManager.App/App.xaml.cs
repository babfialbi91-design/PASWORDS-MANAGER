using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PasswordManager.App;

public partial class App : Application
{
    private const string MutexName = @"Local\PasswordManager.SingleInstance.1";

    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        Localization.Instance.Language = AppSettings.Load().Language;

        if (!AcquireSingleInstance())
        {
            try
            {
                MessageBox.Show(
                    Localization.Get("Main_AlreadyRunning"),
                    Localization.Get("Common_Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch { /* تجاهل */ }
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log("AppDomain", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log("UnobservedTask", args.Exception);
            args.SetObserved();
        };

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SecureClipboard.Clear();
        try { _mutex?.Dispose(); } catch { /* تجاهل */ }
        base.OnExit(e);
    }

    private static bool AcquireSingleInstance()
    {
        try
        {
            var mutex = new Mutex(true, MutexName, out var createdNew);
            if (!createdNew)
            {
                mutex.Dispose();
                return false;
            }
            _mutex = mutex;
            return true;
        }
        catch
        {
            // إذا تعذر إنشاء المُعدِّد، لا نمنع تشغيل التطبيق.
            return true;
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log("Dispatcher", e.Exception);
        try
        {
            MessageBox.Show(
                string.Format(Localization.Get("Main_ErrorFatal"), e.Exception.Message),
                Localization.Get("Common_Error"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch { /* تجاهل */ }
        e.Handled = true;
    }

    internal static void Log(string source, Exception? ex)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PasswordManager");
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {ex}\n");
        }
        catch { /* تجاهل */ }
    }
}
