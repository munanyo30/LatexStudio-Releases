using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LatexStudio.Core;
using LatexStudio.Models;
using LatexStudio.Services;
using Microsoft.Win32;

namespace LatexStudio.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly LatexGenerator generator = new();
    private readonly ProjectStore projectStore = new();
    private readonly PdfBuildService pdfBuildService = new();
    private readonly Stack<string> undo = [];
    private readonly Stack<string> redo = [];
    private readonly string autosavePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LatexStudio",
        "autosave.latexstudio.json");

    [ObservableProperty] private AcademicDocument document = new AcademicDocument();
    [ObservableProperty] private DocumentElement? selectedElement;
    [ObservableProperty] private string latexCode = "";
    [ObservableProperty] private string pdfPath = "";
    [ObservableProperty] private string status = "Pronto";
    [ObservableProperty] private string licenseInfo = "Licença Ativa";
    [ObservableProperty] private bool isDarkTheme;
    [ObservableProperty] private int selectedProjectIndex;
    [ObservableProperty] private LatexTemplate? selectedTemplate;
    private bool isRegenerating;
    private bool isRestoring;

    public ObservableCollection<AcademicDocument> Projects { get; } = [];
    public IReadOnlyList<LatexTemplate> Templates { get; }
    public IReadOnlyList<LatexEngineKind> Engines { get; } = Enum.GetValues<LatexEngineKind>();
    public IReadOnlyList<ImageLayoutMode> ImageLayouts { get; } = Enum.GetValues<ImageLayoutMode>();
    public IReadOnlyList<ListKind> ListKinds { get; } = Enum.GetValues<ListKind>();
    public IReadOnlyList<ChartKind> ChartKinds { get; } = Enum.GetValues<ChartKind>();
    public IReadOnlyList<SectionLevel> SectionLevels { get; } = Enum.GetValues<SectionLevel>();
    public IReadOnlyList<LatexStudio.Models.TextAlignment> TextAlignments { get; } = Enum.GetValues<LatexStudio.Models.TextAlignment>();
    public IReadOnlyList<TheoremKind> TheoremKinds { get; } = Enum.GetValues<TheoremKind>();
    public IReadOnlyList<LatexSymbol> GreekLetters { get; } = LatexSymbolData.GreekLetters;
    public IReadOnlyList<LatexSymbol> Operators { get; } = LatexSymbolData.Operators;
    public IReadOnlyList<LatexSymbol> Relations { get; } = LatexSymbolData.Relations;
    public IReadOnlyList<LatexSymbol> Structures { get; } = LatexSymbolData.Structures;
    public IReadOnlyList<string> ProgrammingLanguages { get; } = new List<string>
    {
        "C", "C++", "C#", "Java", "Python", "JavaScript", "TypeScript", "PHP", "Ruby", "Swift",
        "Go", "Rust", "Kotlin", "Scala", "Haskell", "SQL", "HTML", "CSS", "LaTeX", "Bash",
        "R", "Matlab", "Fortran", "Assembly", "Lua", "Perl", "VHDL", "Verilog"
    }.OrderBy(x => x).ToList();

    [ObservableProperty] private bool isUpdateAvailable;
    [ObservableProperty] private string updateVersion = "";
    [ObservableProperty] private string updateUrl = "";

    private readonly UpdateService updateService = new();
    private readonly System.Windows.Threading.DispatcherTimer undoTimer;

    public MainViewModel()
    {
        undoTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        undoTimer.Tick += (s, e) => {
            undoTimer.Stop();
            CaptureUndo();
        };

        Templates = new TemplateService().LoadTemplates();
        SelectedTemplate = Templates.FirstOrDefault();

        // Load global settings
        var settings = SettingsStore.Load();
        IsDarkTheme = settings.IsDarkTheme;
        Document.Engine = Enum.TryParse<LatexEngineKind>(settings.DefaultEngine, out var engine) ? engine : LatexEngineKind.PdfLatex;
        Document.CustomEnginePath = settings.CustomEnginePath;

        Projects.Add(Document);
        SelectedElement = Document.Elements.FirstOrDefault();
        HookDocument(Document);
        CaptureUndo();
        Regenerate();
        CheckForUpdates();
    }

    private async void CheckForUpdates()
    {
        var (available, version, url) = await updateService.CheckForUpdatesAsync();
        IsUpdateAvailable = available;
        UpdateVersion = version;
        UpdateUrl = url;
    }

    [RelayCommand]
    private void DownloadUpdate()
    {
        if (!string.IsNullOrEmpty(UpdateUrl))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = UpdateUrl,
                UseShellExecute = true
            });
        }
    }
    partial void OnIsDarkThemeChanged(bool value) => SaveAppSettings();

    private void SaveAppSettings()
    {
        SettingsStore.Save(new AppSettings
        {
            IsDarkTheme = IsDarkTheme,
            DefaultEngine = Document.Engine.ToString(),
            CustomEnginePath = Document.CustomEnginePath ?? ""
        });
    }

    partial void OnSelectedTemplateChanged(LatexTemplate? value) => Regenerate();

    partial void OnDocumentChanged(AcademicDocument value)
    {
        HookDocument(value);
        SelectedElement = value.Elements.FirstOrDefault();
        Regenerate();
        SaveAppSettings();
    }

    partial void OnSelectedProjectIndexChanged(int value)
    {
        if (value >= 0 && value < Projects.Count)
        {
            Document = Projects[value];
            IsProjectView = true;
            IsSettingsOpen = false;
        }
    }

    [RelayCommand]
    private void NewProject()
    {
        var project = new AcademicDocument();
        project.Name = $"Projeto {Projects.Count + 1}";
        Projects.Add(project);
        SelectedProjectIndex = Projects.Count - 1;
        CaptureUndo();
    }

    [RelayCommand]
    private void AddTable()
    {
        var table = TableElement.CreateRaw();
        table.Title = $"Tabela {Document.Elements.OfType<TableElement>().Count() + 1}";
        AddElement(table);
    }

    [ObservableProperty] private bool isSettingsOpen;
    [ObservableProperty] private bool isProjectView = true;

    partial void OnIsSettingsOpenChanged(bool value)
    {
        if (value)
        {
            IsProjectView = false;
            SelectedProjectIndex = -1;
        }
    }

    [RelayCommand]
    private void OpenSettings() => IsSettingsOpen = true;

    [RelayCommand]
    private void AddImage()
    {
        var image = new ImageElement { Title = $"Figura {Document.Elements.OfType<ImageElement>().Count() + 1}", Caption = "Legenda da figura." };
        image.Images.Add(new ImageItem { Path = "assets/imagem.pdf", Caption = "Subfigura" });
        AddElement(image);
    }

    [RelayCommand]
    private void AddList()
    {
        var list = ListElement.CreateExample();
        list.Title = $"Lista {Document.Elements.OfType<ListElement>().Count() + 1}";
        AddElement(list);
    }

    [RelayCommand]
    private void AddChart()
    {
        var chart = ChartElement.CreateExample();
        chart.Title = $"Gráfico {Document.Elements.OfType<ChartElement>().Count() + 1}";
        AddElement(chart);
    }

    [RelayCommand]
    private void AddText()
    {
        var text = new TextElement { Title = "Novo Parágrafo", Content = "Escreva o seu texto aqui..." };
        AddElement(text);
    }

    [RelayCommand]
    private void AddEquation()
    {
        var equation = new EquationElement { Title = "Nova Equação", Formula = "a^2 + b^2 = c^2" };
        AddElement(equation);
    }

    [RelayCommand]
    private void AddBibliography()
    {
        var bib = BibliographyElement.CreateExample();
        AddElement(bib);
    }

    [RelayCommand]
    private void AddCode()
    {
        var code = CodeElement.CreateExample();
        AddElement(code);
    }

    [RelayCommand]
    private void AddTheorem()
    {
        var theorem = TheoremElement.CreateExample();
        AddElement(theorem);
    }

    [RelayCommand]
    private void AddCustomCode()
    {
        var custom = CustomCodeElement.CreateExample();
        AddElement(custom);
    }

    [RelayCommand]
    private void InsertEquationSymbol(string symbol)
    {
        if (SelectedElement is EquationElement equation)
        {
            equation.Formula += symbol;
            CaptureUndo();
            Regenerate();
        }
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        if (SelectedElement == null) return;
        int index = Document.Elements.IndexOf(SelectedElement);
        if (index > 0)
        {
            Document.Elements.Move(index, index - 1);
            CaptureUndo();
            Regenerate();
            MoveUpCommand.NotifyCanExecuteChanged();
            MoveDownCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        if (SelectedElement == null) return;
        int index = Document.Elements.IndexOf(SelectedElement);
        if (index < Document.Elements.Count - 1)
        {
            Document.Elements.Move(index, index + 1);
            CaptureUndo();
            Regenerate();
            MoveUpCommand.NotifyCanExecuteChanged();
            MoveDownCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanMoveUp() => SelectedElement != null && Document.Elements.IndexOf(SelectedElement) > 0;
    private bool CanMoveDown() => SelectedElement != null && Document.Elements.IndexOf(SelectedElement) < Document.Elements.Count - 1;

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedElement is null) return;
        Document.Elements.Remove(SelectedElement);
        SelectedElement = Document.Elements.FirstOrDefault();
        CaptureUndo();
        Regenerate();
    }

    [RelayCommand]
    private void AddRow()
    {
        if (SelectedElement is TableElement table)
        {
            table.AddRow();
            CaptureUndo();
            Regenerate();
        }
    }

    [RelayCommand]
    private void RemoveRow(TableCell? cell = null)
    {
        if (SelectedElement is TableElement table)
        {
            if (cell != null) UpdateSelection(table, cell);
            table.RemoveRow();
            CaptureUndo();
            Regenerate();
        }
    }

    [RelayCommand]
    private void AddColumn()
    {
        if (SelectedElement is TableElement table)
        {
            table.AddColumn("");
            CaptureUndo();
            Regenerate();
        }
    }

    [RelayCommand]
    private void RemoveColumn(TableCell? cell = null)
    {
        if (SelectedElement is TableElement table)
        {
            if (cell != null) UpdateSelection(table, cell);
            table.RemoveColumn();
            CaptureUndo();
            Regenerate();
        }
    }

    [RelayCommand]
    private void MergeCell(TableCell? cell = null)
    {
        if (SelectedElement is TableElement table)
        {
            if (cell != null) UpdateSelection(table, cell);
            table.MergeSelectedRight();
            CaptureUndo();
            Regenerate();
        }
    }

    [RelayCommand]
    private void MergeCellDown(TableCell? cell = null)
    {
        if (SelectedElement is TableElement table)
        {
            if (cell != null) UpdateSelection(table, cell);
            table.MergeSelectedDown();
            CaptureUndo();
            Regenerate();
        }
    }

    [RelayCommand]
    private void SplitCell(TableCell? cell = null)
    {
        if (SelectedElement is TableElement table)
        {
            if (cell != null) UpdateSelection(table, cell);
            table.SplitSelected();
            CaptureUndo();
            Regenerate();
        }
    }

    private void UpdateSelection(TableElement table, TableCell cell)
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
[RelayCommand]
private void AddImagePlaceholder()
{
    if (SelectedElement is not ImageElement image) return;

    var newItem = new ImageItem { Path = "caminho/para/imagem.png", Caption = "Legenda da Imagem" };
    image.Images.Add(newItem);
    Regenerate();
    CaptureUndo();
}[RelayCommand]
private void RemoveImage(ImageItem item)
{
    if (SelectedElement is ImageElement image)
    {
        image.Images.Remove(item);
        Regenerate();
        CaptureUndo();
    }
}

[RelayCommand]
private void MoveImageUp(ImageItem item)
{
    if (SelectedElement is ImageElement image)
    {
        int index = image.Images.IndexOf(item);
        if (index > 0)
        {
            image.Images.Move(index, index - 1);
            Regenerate();
            CaptureUndo();
        }
    }
}

[RelayCommand]
private void MoveImageDown(ImageItem item)
{
    if (SelectedElement is ImageElement image)
    {
        int index = image.Images.IndexOf(item);
        if (index < image.Images.Count - 1)
        {
            image.Images.Move(index, index + 1);
            Regenerate();
            CaptureUndo();
        }
    }
}
    [RelayCommand]
    private void AddListItem()
    {
        if (SelectedElement is ListElement list)
        {
            list.Items.Add(new ListItemNode { Text = "Novo item" });
            Regenerate();
            CaptureUndo();
        }
    }

    [RelayCommand]
    private void RemoveListItem(ListItemNode item)
    {
        if (SelectedElement is ListElement list)
        {
            list.Items.Remove(item);
            Regenerate();
            CaptureUndo();
        }
    }

    [RelayCommand]
    private void AddChartSeries()
    {
        if (SelectedElement is ChartElement chart)
        {
            chart.Series.Add(new ChartSeries { Name = "Nova Série", Color = "blue", Values = { 10, 20 } });
            Regenerate();
            CaptureUndo();
        }
    }

    [RelayCommand]
    private void RemoveChartSeries(ChartSeries series)
    {
        if (SelectedElement is ChartElement chart)
        {
            chart.Series.Remove(series);
            Regenerate();
            CaptureUndo();
        }
    }

    partial void OnSelectedElementChanged(DocumentElement? value)
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Regenerate()
    {
        if (isRegenerating) return;
        isRegenerating = true;
        try
        {
            LatexCode = generator.Generate(Document, SelectedTemplate).Code;
            Status = $"Preview atualizado às {DateTime.Now:HH:mm:ss}";
            SaveAutosave();
        }
        finally
        {
            isRegenerating = false;
        }
    }

    [RelayCommand]
    private void CopyLatex()
    {
        Clipboard.SetText(LatexCode);
        Status = "Código LaTeX copiado.";
    }

    [RelayCommand]
    private void VerifyLicense()
    {
        var licensePath = LicenseService.GetLicensePath();
        if (File.Exists(licensePath))
        {
            var key = File.ReadAllText(licensePath);
            var result = LicenseService.ValidateWithDefaultKey(key);
            if (result.IsValid)
            {
                LicenseInfo = result.Message;
                MessageBox.Show(result.Message, "Estado da Licença", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                LicenseInfo = "Licença Inválida";
                MessageBox.Show(result.Message, "Licença Inválida", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }
        else
        {
            LicenseInfo = "Sem Licença";
            Application.Current.Shutdown();
        }
    }

    [RelayCommand]
    private void ExportToOverleaf()
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "overleaf_export.html");
            var html = $@"
            <html>
            <body onload='document.forms[0].submit()'>
                <form action='https://www.overleaf.com/docs' method='POST'>
                    <input type='hidden' name='snip' value='{WebUtility.HtmlEncode(LatexCode)}'>
                </form>
                <p>A redirecionar para o Overleaf...</p>
            </body>
            </html>";
            File.WriteAllText(tempPath, html);
            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Status = $"Erro ao exportar para Overleaf: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportTex()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Exportar .tex",
            Filter = "LaTeX|*.tex",
            FileName = $"{SanitizeFileName(Document.Name)}.tex"
        };

        if (dialog.ShowDialog() != true) return;
        File.WriteAllText(dialog.FileName, LatexCode);
        Status = $"Exportado: {dialog.FileName}";
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Exportar PDF via LaTeX",
            Filter = "LaTeX|*.tex",
            FileName = $"{SanitizeFileName(Document.Name)}.tex"
        };

        if (dialog.ShowDialog() != true) return;
        File.WriteAllText(dialog.FileName, LatexCode);
        try
        {
            var pdf = await pdfBuildService.BuildAsync(Document, dialog.FileName);
            PdfPath = new Uri(Path.GetFullPath(pdf)).AbsoluteUri;
            Status = $"PDF gerado: {pdf}";
        }
        catch (Exception ex)
        {
            Status = $"Falha ao gerar PDF: {ex.Message.Split(Environment.NewLine).FirstOrDefault()}";
        }
    }

    [RelayCommand]
    private void SaveProject()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Guardar projeto",
            Filter = "Latex Studio|*.latexstudio.json",
            FileName = $"{SanitizeFileName(Document.Name)}.latexstudio.json"
        };

        if (dialog.ShowDialog() == true)
        {
            projectStore.Save(Document, dialog.FileName);
            Status = $"Projeto guardado: {dialog.FileName}";
        }
    }

    [RelayCommand]
    private void OpenProject()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Abrir projeto",
            Filter = "Latex Studio|*.latexstudio.json|JSON|*.json"
        };

        if (dialog.ShowDialog() != true) return;
        var loaded = projectStore.Load(dialog.FileName);
        Projects.Add(loaded);
        SelectedProjectIndex = Projects.Count - 1;
        CaptureUndo();
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (undo.Count <= 1) return;
        
        // Push current state to redo
        redo.Push(undo.Pop());
        
        // Restore previous state
        Restore(undo.Peek());
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (redo.Count == 0) return;
        
        var snapshot = redo.Pop();
        undo.Push(snapshot);
        Restore(snapshot);
    }

    public bool CanUndo() => undo.Count > 1;
    public bool CanRedo() => redo.Count > 0;

    public void ImportDroppedImages(IEnumerable<string> files)
    {
        var image = SelectedElement as ImageElement;
        if (image is null)
        {
            AddImage();
            image = SelectedElement as ImageElement;
        }

        if (image is null) return;
        foreach (var file in files)
        {
            image.Images.Add(new ImageItem { Path = file, Caption = Path.GetFileNameWithoutExtension(file) });
        }
        Regenerate();
        CaptureUndo();
    }

    private void AddElement(DocumentElement element)
    {
        Document.Elements.Add(element);
        SelectedElement = element;
        Regenerate();
        CaptureUndo();
    }

    private void HookDocument(AcademicDocument document)
    {
        document.PropertyChanged += OnElementPropertyChanged;
        document.Elements.CollectionChanged += OnElementsChanged;
        foreach (var element in document.Elements) HookElement(element);
    }

    private void OnElementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (DocumentElement element in e.NewItems) HookElement(element);
        }
        Regenerate();
    }

    private void HookElement(DocumentElement element)
    {
        element.PropertyChanged += OnElementPropertyChanged;
        
        switch (element)
        {
            case TableElement table:
                table.Rows.CollectionChanged += (_, _) => Regenerate();
                foreach (var row in table.Rows)
                {
                    row.PropertyChanged += OnElementPropertyChanged;
                    row.Cells.CollectionChanged += (_, _) => Regenerate();
                    foreach (var cell in row.Cells) cell.PropertyChanged += OnElementPropertyChanged;
                }
                break;

            case ImageElement image:
                image.Images.CollectionChanged += (s, e) => {
                    if (e.NewItems != null) foreach (ImageItem img in e.NewItems) img.PropertyChanged += OnElementPropertyChanged;
                    Regenerate();
                };
                foreach (var img in image.Images) img.PropertyChanged += OnElementPropertyChanged;
                break;

            case ListElement list:
                list.Items.CollectionChanged += (s, e) => {
                    if (e.NewItems != null) foreach (ListItemNode item in e.NewItems) HookListItem(item);
                    Regenerate();
                };
                foreach (var item in list.Items) HookListItem(item);
                break;

            case ChartElement chart:
                chart.Series.CollectionChanged += (s, e) => {
                    if (e.NewItems != null) foreach (ChartSeries series in e.NewItems) 
                    {
                        series.PropertyChanged += OnElementPropertyChanged;
                        series.Values.CollectionChanged += (_, _) => Regenerate();
                    }
                    Regenerate();
                };
                foreach (var series in chart.Series)
                {
                    series.PropertyChanged += OnElementPropertyChanged;
                    series.Values.CollectionChanged += (_, _) => Regenerate();
                }
                break;
        }
    }

    private void HookListItem(ListItemNode item)
    {
        item.PropertyChanged += OnElementPropertyChanged;
        item.Children.CollectionChanged += (_, _) => Regenerate();
        foreach (var child in item.Children) HookListItem(child);
    }

    private void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (isRestoring) return;
        Regenerate();
        
        // Don't capture undo for status/latex updates to avoid infinite loops or overhead
        if (e.PropertyName is not (nameof(LatexCode) or nameof(Status) or nameof(IsUpdateAvailable)))
        {
            undoTimer.Stop();
            undoTimer.Start();
        }

        if (e.PropertyName is nameof(AcademicDocument.Engine) or nameof(AcademicDocument.CustomEnginePath))
        {
            SaveAppSettings();
        }
    }

    private void CaptureUndo()
    {
        if (isRestoring) return;
        undo.Push(projectStore.Serialize(Document));
        redo.Clear();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void Restore(string snapshot)
    {
        isRestoring = true;
        try
        {
            var oldSelectedId = SelectedElement?.Id;
            
            Document = projectStore.Deserialize(snapshot);
            
            // Re-sync selection using Guid ID
            if (oldSelectedId != null)
            {
                SelectedElement = Document.Elements.FirstOrDefault(e => e.Id == oldSelectedId);
            }
            else
            {
                SelectedElement = Document.Elements.FirstOrDefault();
            }

            if (SelectedProjectIndex >= 0 && SelectedProjectIndex < Projects.Count)
            {
                Projects[SelectedProjectIndex] = Document;
            }
            else
            {
                Projects.Clear();
                Projects.Add(Document);
                SelectedProjectIndex = 0;
            }

            Regenerate();
        }
        finally
        {
            isRestoring = false;
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }
    }

    private void SaveAutosave()
    {
        try
        {
            projectStore.Save(Document, autosavePath);
        }
        catch
        {
            // Autosave must stay silent; explicit export still reports errors through the UI.
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '-');
        }
        return string.IsNullOrWhiteSpace(name) ? "documento" : name;
    }
}
