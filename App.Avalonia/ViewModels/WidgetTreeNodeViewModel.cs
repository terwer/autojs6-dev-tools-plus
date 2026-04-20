using Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Avalonia;

namespace App.Avalonia.ViewModels;

public partial class WidgetTreeNodeViewModel : ViewModelBase
{
    public WidgetTreeNodeViewModel(WidgetNode node, string title, string? subtitle = null, int depth = 0)
    {
        Node = node;
        Title = title;
        Subtitle = subtitle;
        Depth = depth;
    }

    public WidgetNode Node { get; }

    public string Title { get; }

    public string? Subtitle { get; }

    public int Depth { get; }

    public bool HasChildren => Node.Children.Count > 0;

    public Thickness Indent => new(Depth * 16, 0, 0, 0);

    public string ExpandGlyph => IsExpanded ? "▾" : "▸";

    public ObservableCollection<WidgetTreeNodeViewModel> Children { get; } = [];

    [ObservableProperty]
    private bool _isExpanded;
}
