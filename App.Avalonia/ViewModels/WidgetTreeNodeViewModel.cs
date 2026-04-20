using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace App.Avalonia.ViewModels;

public partial class WidgetTreeNodeViewModel : ViewModelBase
{
    public WidgetTreeNodeViewModel(string title, string? subtitle = null)
    {
        Title = title;
        Subtitle = subtitle;
    }

    public string Title { get; }

    public string? Subtitle { get; }

    public ObservableCollection<WidgetTreeNodeViewModel> Children { get; } = [];

    [ObservableProperty]
    private bool _isExpanded;
}
