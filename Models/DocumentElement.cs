using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public abstract partial class DocumentElement : ObservableObject
{
    [ObservableProperty] private Guid id = Guid.NewGuid();
    [ObservableProperty] private string title = "";
    [ObservableProperty] private string caption = "";
    [ObservableProperty] private string label = "";
    [ObservableProperty] private bool includeInExport = true;

    public abstract DocumentElementKind Kind { get; }
    public abstract string DisplayName { get; }

    partial void OnTitleChanged(string value) => OnPropertyChanged(nameof(DisplayName));
}
