using System.Windows;

namespace PasswordManager.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Localization.Instance.Language = AppSettings.Load().Language;
        base.OnStartup(e);
    }
}
