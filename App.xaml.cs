using System.IO;
using System.Windows;
using LatexStudio.Core;
using LatexStudio.Views;

namespace LatexStudio;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var licensePath = LicenseService.GetLicensePath();
        bool isAuthorized = false;

        if (File.Exists(licensePath))
        {
            var key = File.ReadAllText(licensePath);
            var result = LicenseService.ValidateWithDefaultKey(key);
            if (result.IsValid)
            {
                isAuthorized = true;
            }
        }

        if (!isAuthorized)
        {
            var activation = new ActivationWindow();
            if (activation.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
        }

        var main = new MainWindow();
        main.Show();
    }
}
