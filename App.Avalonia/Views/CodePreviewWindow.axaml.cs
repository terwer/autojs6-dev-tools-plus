using Avalonia.Controls;
using Avalonia.Interactivity;
using Core.Abstractions;
using Core.Abstractions.Desktop;
using Core.Models.Desktop;

namespace App.Avalonia.Views;

public partial class CodePreviewWindow : Window
{
    private readonly IClipboardService? _clipboardService;
    private readonly IFileSaveDialogService? _fileSaveDialogService;
    private readonly ICodeGenerator? _codeGenerator;

    public CodePreviewWindow()
        : this(string.Empty, null, null, null)
    {
    }

    public CodePreviewWindow(
        string code,
        IClipboardService? clipboardService,
        IFileSaveDialogService? fileSaveDialogService,
        ICodeGenerator? codeGenerator)
    {
        _clipboardService = clipboardService;
        _fileSaveDialogService = fileSaveDialogService;
        _codeGenerator = codeGenerator;
        InitializeComponent();
        CodeTextBox.Text = code;
        FormatButton.Click += OnFormatButtonClick;
        CopyButton.Click += OnCopyButtonClick;
        ExportButton.Click += OnExportButtonClick;
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

    private void OnFormatButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_codeGenerator is null || string.IsNullOrWhiteSpace(CodeTextBox.Text))
        {
            return;
        }

        CodeTextBox.Text = _codeGenerator.FormatCode(CodeTextBox.Text);
    }

    private async void OnExportButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_fileSaveDialogService is null || string.IsNullOrWhiteSpace(CodeTextBox.Text))
        {
            return;
        }

        var result = await _fileSaveDialogService.SaveFileAsync(new SaveFileRequest(
            "导出 AutoJS6 脚本",
            "autojs6-script.js",
            ".js",
            [new FileDialogFilter("JavaScript 文件", ["*.js"])]));

        if (!result.Confirmed || string.IsNullOrWhiteSpace(result.FilePath))
        {
            return;
        }

        await File.WriteAllTextAsync(result.FilePath, CodeTextBox.Text);
    }
}
