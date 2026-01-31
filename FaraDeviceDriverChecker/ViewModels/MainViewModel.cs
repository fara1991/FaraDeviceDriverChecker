namespace FaraDeviceDriverChecker.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Models;
using Services;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly DeviceService _deviceService = new();
    private bool _isLoading;
    private bool _hasScanned;
    private string _statusMessage = "デバイスカテゴリを選択してスキャンしてください";

    public ObservableCollection<DeviceCategory> Categories { get; } = new(DeviceCategory.GetAll());
    public ObservableCollection<DeviceInfo> Devices { get; } = [];

    public bool HasSelectedCategories => Categories.Any(c => c.IsSelected);

    public bool IsAllSelected
    {
        get => Categories.All(c => c.IsSelected);
        set
        {
            foreach (var category in Categories)
            {
                category.IsSelected = value;
            }
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

    public bool HasScanned
    {
        get => _hasScanned;
        private set
        {
            _hasScanned = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand ScanCommand => new RelayCommand(async () => await ScanDevicesAsync(), () => IsNotLoading && HasSelectedCategories);
    public ICommand RunUpdateCommand => new RelayCommand(async () => await RunUpdateAsync(), () => IsNotLoading && HasScanned);
    public ICommand OpenSettingsCommand => new RelayCommand(() => _deviceService.OpenWindowsUpdateSettings());

    private async Task ScanDevicesAsync()
    {
        var selectedCategories = Categories.Where(c => c.IsSelected).ToList();
        if (selectedCategories.Count == 0) return;

        IsLoading = true;
        Devices.Clear();

        try
        {
            var allClasses = selectedCategories.SelectMany(c => c.Classes).ToArray();
            var categoryNames = string.Join(", ", selectedCategories.Select(c => c.Name));
            StatusMessage = $"{categoryNames}をスキャン中...";

            var devices = await Task.Run(() => _deviceService.GetDevices(allClasses));

            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            var problemCount = devices.Count(d => d.HasProblem);
            StatusMessage = $"{devices.Count}個のデバイス / 問題: {problemCount}個";
            HasScanned = true;
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
        StatusMessage = "Updateを実行中...";

        try
        {
            var (success, message) = await _deviceService.RunWindowsUpdateAsync();
            StatusMessage = message;

            if (success)
            {
                MessageBox.Show("Updateが完了しました", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
            MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
