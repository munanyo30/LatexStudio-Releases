using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class TableElement : DocumentElement
{
    [ObservableProperty] private bool hasHeader = true;
    [ObservableProperty] private TableLayoutMode layoutMode = TableLayoutMode.Single;
    [ObservableProperty] private bool useTabularX = true;
    [ObservableProperty] private bool useBooktabs = true;
    [ObservableProperty] private double columnWidth = 92;
    [ObservableProperty] private double rowHeight = 34;
    [ObservableProperty] private int selectedRow;
    [ObservableProperty] private int selectedColumn;
    [ObservableProperty] private double totalWidth;
    [ObservableProperty] private double totalHeight;

    public ObservableCollection<TableRow> Rows { get; } = [];

    public List<TableCell> AllCells => Rows.SelectMany(r => r.Cells).ToList();

    public override DocumentElementKind Kind => DocumentElementKind.Table;
    public override string DisplayName => $"Tabela: {Title}";

    public static TableElement CreateExample()
    {
        var table = new TableElement
        {
            Title = "Resultados",
            Caption = "Resumo dos resultados experimentais.",
            HasHeader = true
        };

        table.AddColumn("Método");
        table.AddColumn("Precisão");
        table.AddColumn("Recall");
        table.AddRow(["Baseline", "82.1\\%", "78.4\\%"]);
        table.AddRow(["Proposto", "91.6\\%", "89.3\\%"]);
        table.UpdateIndices();
        return table;
    }

    public static TableElement CreateRaw()
    {
        var table = new TableElement
        {
            Title = "Nova Tabela",
            Caption = "Legenda da tabela.",
            HasHeader = true
        };

        table.AddRow(); // Adds 3 columns by default
        table.AddRow();
        table.AddRow();
        table.UpdateIndices();
        return table;
    }

    public void UpdateIndices()
    {
        for (int r = 0; r < Rows.Count; r++)
        {
            for (int c = 0; c < Rows[r].Cells.Count; c++)
            {
                Rows[r].Cells[c].RowIndex = r;
                Rows[r].Cells[c].ColumnIndex = c;
            }
        }

        var colCount = Rows.FirstOrDefault()?.Cells.Count ?? 0;
        TotalWidth = colCount * ColumnWidth;
        TotalHeight = Rows.Count * RowHeight;

        OnPropertyChanged(nameof(AllCells));
    }

    public void AddColumn(string header = "Coluna")
    {
        if (Rows.Count == 0)
        {
            AddRow();
            return;
        }

        var index = Math.Clamp(SelectedColumn + 1, 0, Rows[0].Cells.Count);
        for (var r = 0; r < Rows.Count; r++)
        {
            Rows[r].Cells.Insert(index, new TableCell { Text = r == 0 ? header : "", IsHeader = r == 0 && HasHeader });
        }
        UpdateIndices();
        OnPropertyChanged(nameof(Rows));
    }

    public void RemoveColumn()
    {
        if (Rows.Count == 0 || Rows[0].Cells.Count <= 1) return;
        var index = Math.Clamp(SelectedColumn, 0, Rows[0].Cells.Count - 1);
        foreach (var row in Rows)
        {
            row.Cells.RemoveAt(index);
        }
        SelectedColumn = Math.Max(0, index - 1);
        UpdateIndices();
        OnPropertyChanged(nameof(Rows));
    }

    public void AddRow(IEnumerable<string>? values = null)
    {
        var source = values?.ToArray();
        var columns = Math.Max(source?.Length ?? 0, Rows.FirstOrDefault()?.Cells.Count ?? 3);
        
        var row = new TableRow();
        for (var c = 0; c < columns; c++)
        {
            row.Cells.Add(new TableCell
            {
                Text = source != null && c < source.Length ? source[c] : "",
                IsHeader = Rows.Count == 0 && HasHeader
            });
        }

        var index = Rows.Count == 0 ? 0 : Math.Clamp(SelectedRow + 1, 0, Rows.Count);
        Rows.Insert(index, row);
        UpdateIndices();
        OnPropertyChanged(nameof(Rows));
    }

    public void RemoveRow()
    {
        if (Rows.Count <= 1) return;
        var index = Math.Clamp(SelectedRow, 0, Rows.Count - 1);
        Rows.RemoveAt(index);
        SelectedRow = Math.Max(0, index - 1);
        UpdateIndices();
        OnPropertyChanged(nameof(Rows));
    }

    public void ToggleHeader()
    {
        HasHeader = !HasHeader;
        for (var r = 0; r < Rows.Count; r++)
        {
            foreach (var cell in Rows[r].Cells)
            {
                cell.IsHeader = r == 0 && HasHeader;
            }
        }
    }

    public void MergeSelectedRight()
    {
        if (Rows.Count == 0) return;
        var rowIdx = Math.Clamp(SelectedRow, 0, Rows.Count - 1);
        var colIdx = Math.Clamp(SelectedColumn, 0, Rows[rowIdx].Cells.Count - 1);
        
        var currentCell = Rows[rowIdx].Cells[colIdx];
        if (currentCell.IsMergedChild) return;

        // Target: first visible cell to the right
        var targetCol = colIdx + currentCell.ColumnSpan;
        if (targetCol >= Rows[rowIdx].Cells.Count) return;

        var targetCell = Rows[rowIdx].Cells[targetCol];
        
        // Safety check: for a rectangular merge, target must have same RowSpan
        if (targetCell.RowSpan != currentCell.RowSpan) return;

        // Mark ALL cells in the target's area as merged children
        for (int r = 0; r < targetCell.RowSpan; r++)
        {
            for (int c = 0; c < targetCell.ColumnSpan; c++)
            {
                var cell = Rows[rowIdx + r].Cells[targetCol + c];
                cell.IsMergedChild = true;
                if (!string.IsNullOrWhiteSpace(cell.Text) && cell != currentCell)
                {
                    currentCell.Text = $"{currentCell.Text} {cell.Text}".Trim();
                }
            }
        }

        currentCell.ColumnSpan += targetCell.ColumnSpan;
        
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(AllCells));
    }

    public void MergeSelectedDown()
    {
        if (Rows.Count == 0) return;
        var rowIdx = Math.Clamp(SelectedRow, 0, Rows.Count - 1);
        var colIdx = Math.Clamp(SelectedColumn, 0, Rows[rowIdx].Cells.Count - 1);
        
        var currentCell = Rows[rowIdx].Cells[colIdx];
        if (currentCell.IsMergedChild) return;

        // Target: first visible cell below
        var targetRow = rowIdx + currentCell.RowSpan;
        if (targetRow >= Rows.Count) return;

        var targetCell = Rows[targetRow].Cells[colIdx];

        // Safety check: for a rectangular merge, target must have same ColumnSpan
        if (targetCell.ColumnSpan != currentCell.ColumnSpan) return;

        // Mark ALL cells in the target's area as merged children
        for (int r = 0; r < targetCell.RowSpan; r++)
        {
            for (int c = 0; c < targetCell.ColumnSpan; c++)
            {
                var cell = Rows[targetRow + r].Cells[colIdx + c];
                cell.IsMergedChild = true;
                if (!string.IsNullOrWhiteSpace(cell.Text) && cell != currentCell)
                {
                    currentCell.Text = $"{currentCell.Text} {cell.Text}".Trim();
                }
            }
        }

        currentCell.RowSpan += targetCell.RowSpan;

        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(AllCells));
    }

    public void SplitSelected()
    {
        if (Rows.Count == 0) return;
        var row = Math.Clamp(SelectedRow, 0, Rows.Count - 1);
        var col = Math.Clamp(SelectedColumn, 0, Rows[row].Cells.Count - 1);
        var current = Rows[row].Cells[col];

        // Cache the spans because they change during the loop!
        int rSpan = current.RowSpan;
        int cSpan = current.ColumnSpan;

        for (int r = 0; r < rSpan; r++)
        {
            for (int c = 0; c < cSpan; c++)
            {
                if (row + r < Rows.Count && col + c < Rows[row + r].Cells.Count)
                {
                    var cell = Rows[row + r].Cells[col + c];
                    cell.IsMergedChild = false;
                    cell.RowSpan = 1;
                    cell.ColumnSpan = 1;
                }
            }
        }

        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(AllCells));
    }
}
