using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using LatexStudio.Core;
using System.Collections.ObjectModel;
using System.Linq;

namespace LatexStudio.LicenseGenerator;

public partial class MainWindow : Window
{
    private const string PrivateKey = "MIIEowIBAAKCAQEA2/nIHIuxv3TnWUTxdss1WXGTQCCwUHy/mDK9Sr793ZlbcDKkwImuj8/zjdeblGvKgKnZInlUNeDQoqrglPG/FyCxPZQcpG49MnbOyBeqoQ2aWlkoycFvruYCa+bc+q2+sPrgcYnFZQqtc532+O0Hjr5QEte/HJoKLKKSAlCgBcC/D2m4vQfuS5UFEdKxmCGkzhFKXxH6kYJ0NlpGBQuz91YNzHJWpo7pyz1EuB6WhtZLZ4mAcXCjVBQ1LgS20iM1GKHGC+HRDjLZbryVOLxSQlf27oZZwM8bmRP/442IVcRuH51EhEW/wB8MQdxAhDOmWCkAJPW/CiCunciTNKVGZQIDAQABAoIBAEYZypi2N9XQlm1PWDfIOF8hn5BfGnD74D7LZKFuTg6RcZ9GtFQbTELOPUplfFIHK8hm2ChxS9HDrBk6pUkx5Pik1XbPPXV1IBF15R3E1P5wDFFgemwZNdMhv4HNV3yXY96YhHAKPJqflXjzzSG0v6TQp0np8SBXQojI4gjU0roiA9SNBiRg/BNq7pad1LIDvFGzbuFr9SobD5WZhKxAQw+Zn69tqBoy7vShXTGiK7bwMYIR8HX8+WGTp0F+D/xD7IAGpLV94u1ILmnq/SgBvP8ZD21VJGSue5cNzohzOL8/LrhoR5K38olfMxQnwzy2p8P+huEMgGwxXzFKTpaaCT0CgYEA3Cr5BZUdne0/5P8YN4eF16m2gxzagirgaBWV8nXIugd3s9WPYgqxidjTRRoMWziKA5G/ypNV393b81dyI4UtPQXzWHw2Vvll6gZU0q8KmTmNBBS/3fpZtaw3VzQGZOJ56USd1giK25z/dRnctOrfoLMUHbRqEGR3gVCg4oXVuB8CgYEA/8bNmpx7yBVjL0cytTKLa8yOcwOR7715r9g8x0x4g4F33LQNzTPSYLl5PKLGDwSOr9NLTbQRYcYzPvGPVWIq5nWcVsjxOtFVV2yz6lzk3IjMVu3e7bMUsdFWuaWjlgEQOONL0jkzoBqFlJcN1TZWTu/1raB32xvy//CtdXeuQPsCgYEAg3VZKP8nJvPQ8c9qy0UtIl8gLdsdkRk+0ocI+DNhvcnVrFf4e+a8qP0A8MKj3Be/OHBfHvqoDLowqXRuH01WfJg/+3Z3D6lsM7bCEOYZIvIdA/HLuiPQSsxgYr4aj0Q23JRu1axWFNkCIw5lHNUc35vth+sAZXdHb7wPxBF7UasCgYAp5XeiPsWr1Y0EnoX41kmuI7ilsE0EVyYKnsH951HKQLfQiBHGrhlkzJzGSOdmaFmzBUpNdRRvzpZwVgMjekwKqfMs6AdTmhx/kIe7+PFoCmRmTNp6Vx4prl+lUzODi7MLjnfJ/KKvNAjzePbEGP10Oqwkf6eCVYatU9Al8K+zZwKBgBcB9bLeTlHS4078OGxSPDWxblfMw7KYsccfCnloQAfFpVX/BqAC79btPPCM5Cg9ognmkpAWp653CqemGY9VnBk/ILBdvdzegFIoj8+YzAwaWJWWZF2TNWTeino0TZHgtn5GNAHH49DFl3gAJQgFp3TkJE0NFq6raL6ZsRrHAV4F";
    private LicenseModel? generatedLicense;
    public ObservableCollection<LicenseRecord> History { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        HistoryGrid.ItemsSource = History;
        LoadHistory();
    }

    private void LoadHistory()
    {
        History.Clear();
        foreach (var record in LicenseDatabase.GetAll())
        {
            History.Add(record);
        }
    }

    private void OnGenerateLicense(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DeviceIdInput.Text))
        {
            MessageBox.Show("Por favor, introduza o Device ID do cliente.", "Dados em Falta", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var expiryDate = DurationInput.SelectedIndex switch
        {
            0 => DateTime.Now.AddDays(7),   // Trial 7
            1 => DateTime.Now.AddDays(15),  // Trial 15
            2 => DateTime.Now.AddMonths(1), // Mensal
            3 => DateTime.Now.AddMonths(3), // 3 Meses
            4 => DateTime.Now.AddMonths(6), // 6 Meses
            5 => DateTime.Now.AddYears(1),  // Anual
            _ => DateTime.Now.AddYears(99)  // Vitalícia
        };

        generatedLicense = new LicenseModel
        {
            ClientName = ClientNameInput.Text,
            DeviceId = DeviceIdInput.Text.Trim(),
            Price = PriceInput.Text.Trim(),
            ExpiryDate = expiryDate.ToString("yyyy-MM-dd")
        };

        // Gerar assinatura
        var json = generatedLicense.ToJson();
        generatedLicense.Signature = CryptoService.Sign(json, PrivateKey);

        // Salvar no histórico (Banco de Dados)
        var record = new LicenseRecord(
            0,
            generatedLicense.ClientName,
            generatedLicense.DeviceId,
            generatedLicense.Price,
            DateTime.Now.ToString("yyyy-MM-dd"),
            generatedLicense.ExpiryDate,
            true // Assume pago por padrão no gerador
        );
        
        LicenseDatabase.Save(record);
        LoadHistory();
        
        MessageBox.Show("Dados da licença gerados e salvos no histórico! Clique em 'SALVAR .LIC' para exportar.", "Licença Gerada", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnSaveLicenseFile(object sender, RoutedEventArgs e)
    {
        if (generatedLicense == null)
        {
            MessageBox.Show("Por favor, gere a licença antes de tentar guardar o ficheiro.", "Ação Necessária", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Filter = "Ficheiro de Licença|*.lic",
            FileName = $"license_{generatedLicense.ClientName.Replace(" ", "_")}.lic"
        };

        if (sfd.ShowDialog() == true)
        {
            var finalJson = generatedLicense.ToJson();
            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(finalJson));
            File.WriteAllText(sfd.FileName, base64);
            LicenseOutput.Text = sfd.FileName;
        }
    }

    private void OnTogglePayment(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LicenseRecord record)
        {
            LicenseDatabase.UpdatePaymentStatus(record.Id, !record.IsPaid);
            LoadHistory();
        }
    }

    private void OnExportCsv(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "vendas_licencas.csv" };
        if (sfd.ShowDialog() == true)
        {
            var lines = History.Select(r => $"{r.Id};{r.ClientName};{r.DeviceId};{r.Price};{r.IssueDate};{r.ExpiryDate};{(r.IsPaid ? "Sim" : "Não")}");
            var header = "ID;Cliente;DeviceID;Preço;Emissão;Expiração;Pago";
            File.WriteAllLines(sfd.FileName, new[] { header }.Concat(lines), System.Text.Encoding.UTF8);
            MessageBox.Show("Relatório exportado com sucesso!", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnExit(object sender, RoutedEventArgs e) => Close();
}
