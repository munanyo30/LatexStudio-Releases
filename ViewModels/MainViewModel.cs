using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [ObservableProperty] private AcademicDocument document = AcademicDocument.CreateSample();
    [ObservableProperty] private DocumentElement? selectedElement;
    [ObservableProperty] private string latexCode = "";
    [ObservableProperty] private string status = "Pronto";
    [ObservableProperty] private bool isDarkTheme;
    [ObservableProperty] private int selectedProjectIndex;
    private bool isRegenerating;

    public ObservableCollection<AcademicDocument> Projects { get; } = [];
    public IReadOnlyList<LatexTemplate> Templates { get; }
    public IReadOnlyList<ImageLayoutMode> ImageLayouts { get; } = Enum.GetValues<ImageLayoutMode>();
    public IReadOnlyList<ListKind> ListKinds { get; } = Enum.GetValues<ListKind>();
    public IReadOnlyList<ChartKind> ChartKinds { get; } = Enum.GetValues<ChartKind>();

    public MainViewModel()
    {
        Templates = new TemplateService().LoadTemplates();
        Projects.Add(Document);
        SelectedElement = Document.Elements.FirstOrDefault();
        HookDocument(Document);
        CaptureUndo();
        Regenerate();
    }

    partial void OnDocumentChanged(AcademicDocument value)
    {
        HookDocument(value);
        SelectedElement = value.Elements.FirstOrDefault();
        Regenerate();
    }

    partial void OnSelectedProjectIndexChanged(int value)
    {
        if (value >= 0 && value < Projects.Count)
        {
            Document = Projects[value];
        }
    }

    [RelayCommand]
    private void NewProject()
    {
        var project = AcademicDocument.CreateSample();
        project.Name = $"Projeto {Projects.Count + 1}";
        Projects.Add(project);
        SelectedProjectIndex = Projects.Count - 1;
        CaptureUndo();
    }

    [RelayCommand]
    private void AddTable()
    {
        var table = TableElement.CreateExample();
        table.Title = $"Tabela {Document.Elements.OfType<TableElement>().Count() + 1}";
        AddElement(table);
    }

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
            table.AddColumn($"Coluna {table.Rows.FirstOrDefault()?.Cells.Count + 1}");
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
    private void AddImageFile()
    {
        if (SelectedElement is not ImageElement image) return;

        var dialog = new OpenFileDialog
        {
            Title = "Selecionar imagem",
            Filter = "Imagens e PDF|*.png;*.jpg;*.jpeg;*.pdf;*.svg|Todos os ficheiros|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true) return;
        foreach (var file in dialog.FileNames)
        {
            image.Images.Add(new ImageItem { Path = file, Caption = Path.GetFileNameWithoutExtension(file) });
        }
        CaptureUndo();
        Regenerate();
    }

    [RelayCommand]
    private void AddListItem()
    {
        if (SelectedElement is ListElement list)
        {
            list.Items.Add(new ListItemNode { Text = "Novo item" });
            CaptureUndo();
            Regenerate();
        }
    }

    [RelayCommand]
    private void Regenerate()
    {
        if (isRegenerating) return;
        isRegenerating = true;
        try
        {
            LatexCode = generator.Generate(Document).Code;
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
        redo.Push(undo.Pop());
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
        CaptureUndo();
        Regenerate();
    }

    private void AddElement(DocumentElement element)
    {
        Document.Elements.Add(element);
        SelectedElement = element;
        CaptureUndo();
        Regenerate();
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
        if (element is TableElement table)
        {
            table.Rows.CollectionChanged += (_, _) => Regenerate();
            foreach (var row in table.Rows)
            {
                row.PropertyChanged += OnElementPropertyChanged;
                row.Cells.CollectionChanged += (_, _) => Regenerate();
                foreach (var cell in row.Cells) cell.PropertyChanged += OnElementPropertyChanged;
            }
        }
    }

    private void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e) => Regenerate();

    private void CaptureUndo()
    {
        undo.Push(projectStore.Serialize(Document));
        redo.Clear();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void Restore(string snapshot)
    {
        Document = projectStore.Deserialize(snapshot);
        
        // Safety check to prevent ArgumentOutOfRangeException
        if (SelectedProjectIndex >= 0 && SelectedProjectIndex < Projects.Count)
        {
            Projects[SelectedProjectIndex] = Document;
        }
        else
        {
            // Fallback: if index is lost, re-add or sync based on the current Document
            Projects.Clear();
            Projects.Add(Document);
            SelectedProjectIndex = 0;
        }

        Regenerate();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
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
