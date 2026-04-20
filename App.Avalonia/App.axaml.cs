using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using App.Avalonia.Services;
using App.Avalonia.ViewModels;
using App.Avalonia.Views;
using Core.Abstractions;
using Core.Abstractions.Desktop;
using Core.Services;
using Infrastructure.Adb;
using Infrastructure.Imaging;
using Infrastructure.Platform;

namespace App.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow? mainWindow = null;
            Func<TopLevel?> topLevelAccessor = () => mainWindow;

            IClipboardService clipboardService = new AvaloniaClipboardService(topLevelAccessor);
            IMessageService messageService = new AvaloniaMessageService(topLevelAccessor);
            IFileSaveDialogService fileSaveDialogService = new AvaloniaFileSaveDialogService(topLevelAccessor);
            IWindowStateService windowStateService = new JsonWindowStateService();
            IWorkbenchFilePickerService filePickerService = new AvaloniaWorkbenchFilePickerService(topLevelAccessor);
            IAdbService adbService = new AdbService();
            IImageProcessingService imageProcessingService = new ImageProcessingService();
            IOpenCVMatchService openCvMatchService = new OpenCvMatchService();
            IUiDumpParser uiDumpParser = new UiDumpParser();
            ICodeGenerator codeGenerator = new AutoJS6CodeGenerator();

            var viewModel = new MainWindowViewModel(
                adbService,
                imageProcessingService,
                openCvMatchService,
                uiDumpParser,
                codeGenerator,
                clipboardService,
                messageService,
                fileSaveDialogService,
                filePickerService);

            mainWindow = new MainWindow(windowStateService, clipboardService)
            {
                DataContext = viewModel
            };

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
