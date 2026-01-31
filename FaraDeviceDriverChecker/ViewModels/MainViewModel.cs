namespace FaraDeviceDriverChecker.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Models;
using Services;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly DeviceService _deviceService = new();
    private DeviceCategory? _selectedCategory;
    private bool _isLoading;
    private string _statusMessage = "デバイスカテゴリを選択してスキャンしてください";

    public ObservableCollection<DeviceCategory> Categories { get; } = new(DeviceCategory.GetAll());
    public ObservableCollection<DeviceInfo> Devices { get; } = [];
    public ObservableCollection<string> AvailableUpdates { get; } = [];

    public DeviceCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotLoading));
        }
    }

    public bool IsNotLoading => !IsLoading;

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand ScanCommand => new RelayCommand(async () => await ScanDevicesAsync(), () => IsNotLoading && SelectedCategory != null);
    public ICommand CheckUpdatesCommand => new RelayCommand(async () => await CheckUpdatesAsync(), () => IsNotLoading);
    public ICommand RunUpdateCommand => new RelayCommand(async () => await RunUpdateAsync(), () => IsNotLoading);
    public ICommand OpenSettingsCommand => new RelayCommand(() => _deviceService.OpenWindowsUpdateSettings());

    private async Task ScanDevicesAsync()
    {
        if (SelectedCategory == null) return;

        IsLoading = true;
        StatusMessage = $"{SelectedCategory.Name}デバイスをスキャン中...";
        Devices.Clear();

        try
        {
            var devices = await Task.Run(() => _deviceService.GetDevices(SelectedCategory.Classes));

            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            var problemCount = devices.Count(d => d.HasProblem);
            StatusMessage = $"{devices.Count}個のデバイスが見つかりました。問題: {problemCount}個";
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CheckUpdatesAsync()
    {
        IsLoading = true;
        StatusMessage = "利用可能な更新を確認中...";
        AvailableUpdates.Clear();

        try
        {
            var updates = await Task.Run(() => _deviceService.GetAvailableDriverUpdates());

            foreach (var update in updates)
            {
                AvailableUpdates.Add(update);
            }

            StatusMessage = updates.Count > 0
                ? $"{updates.Count}件の更新が利用可能です"
                : "利用可能な更新はありません";
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RunUpdateAsync()
    {
        IsLoading = true;
        StatusMessage = "Windows Updateを実行中...";

        try
        {
            var (success, message) = await _deviceService.RunWindowsUpdateAsync();
            StatusMessage = message;
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action? _execute;
    private readonly Func<Task>? _executeAsync;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public async void Execute(object? parameter)
    {
        if (_executeAsync != null)
        {
            await _executeAsync();
        }
        else
        {
            _execute?.Invoke();
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
