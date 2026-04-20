using CommunityToolkit.Mvvm.ComponentModel;

namespace App.Avalonia.ViewModels;

public partial class DeviceListItemViewModel : ViewModelBase
{
    public DeviceListItemViewModel(string serial, string state, string? model = null, string? connectionType = null)
    {
        Serial = serial;
        State = state;
        Model = model;
        ConnectionType = connectionType;
    }

    public string Serial { get; }

    public string State { get; }

    public string? Model { get; }

    public string? ConnectionType { get; }

    [ObservableProperty]
    private bool _isSelected;
}
