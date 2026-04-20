using Avalonia.Controls;
using Avalonia.Interactivity;
using Core.Abstractions.Desktop;

namespace App.Avalonia.Views;

public partial class CodePreviewWindow : Window
{
    private readonly IClipboardService? _clipboardService;

    public CodePreviewWindow()
        : this(string.Empty, null)
    {
    }

    public CodePreviewWindow(string code, IClipboardService? clipboardService)
    {
        _clipboardService = clipboardService;
        InitializeComponent();
        CodeTextBox.Text = code;
        CopyButton.Click += OnCopyButtonClick;
        CloseButton.Click += (_, _) => Close();
    }

    private async void OnCopyButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_clipboardService is null || string.IsNullOrWhiteSpace(CodeTextBox.Text))
        {
            return;
        }

        await _clipboardService.SetTextAsync(CodeTextBox.Text);
    }
}
