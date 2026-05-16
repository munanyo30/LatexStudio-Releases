using System.IO;
using System.Windows;
using Microsoft.Win32;
using LatexStudio.Core;

namespace LatexStudio.Views;

public partial class ActivationWindow : Window
{
    private readonly string licenseFilePath = LicenseService.GetLicensePath();

    public ActivationWindow()
    {
        InitializeComponent();
        DeviceIdText.Text = HardwareIdProvider.GetDeviceId();
    }

    private void OnCopyId(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(DeviceIdText.Text);
        MessageBox.Show("O ID do dispositivo foi copiado para a área de transferência.", "ID Copiado", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnLoadLicenseFile(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "Ficheiro de Licença|*.lic",
            Title = "Selecionar ficheiro .lic"
        };

        if (ofd.ShowDialog() == true)
        {
            LicenseInput.Text = File.ReadAllText(ofd.FileName);
        }
    }

    private void OnActivate(object sender, RoutedEventArgs e)
    {
        var licenseKey = LicenseInput.Text.Trim();
        if (string.IsNullOrEmpty(licenseKey)) 
        {
            MessageBox.Show("Por favor, introduza a chave de ativação ou carregue um ficheiro de licença válido (.lic).", "Dados em Falta", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = LicenseService.ValidateWithDefaultKey(licenseKey);
        if (result.IsValid)
        {
            try
            {
                File.WriteAllText(licenseFilePath, licenseKey);
                MessageBox.Show(result.Message, "Ativação Bem-sucedida", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Não foi possível guardar o ficheiro de licença: " + ex.Message, "Erro de Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show(result.Message, "Erro de Ativação", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnExit(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
