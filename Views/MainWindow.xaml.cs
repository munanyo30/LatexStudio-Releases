using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using LatexStudio.Models;
using LatexStudio.ViewModels;

namespace LatexStudio.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            viewModel.ImportDroppedImages(files);
        }
    }

    private void OnThemeChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;
        var theme = viewModel.IsDarkTheme ? "Dark.xaml" : "Light.xaml";
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var oldTheme = dictionaries.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("Themes/Light.xaml", StringComparison.OrdinalIgnoreCase) == true ||
            d.Source?.OriginalString.Contains("Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase) == true);
        if (oldTheme is not null)
        {
            dictionaries.Remove(oldTheme);
        }
        dictionaries.Insert(0, new ResourceDictionary { Source = new Uri($"pack://application:,,,/LatexStudio.Core;component/Themes/{theme}") });
    }

    private void OnCellGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TableCell cell && DataContext is MainViewModel viewModel)
        {
            if (viewModel.SelectedElement is TableElement table)
            {
                for (int r = 0; r < table.Rows.Count; r++)
                {
                    var cIdx = table.Rows[r].Cells.IndexOf(cell);
                    if (cIdx >= 0)
                    {
                        table.SelectedRow = r;
                        table.SelectedColumn = cIdx;
                        return;
                    }
                }
            }
        }
    }
}
