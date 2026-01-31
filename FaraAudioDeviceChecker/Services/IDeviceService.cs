namespace FaraAudioDeviceChecker.Services;

using Models;

public interface IDeviceService
{
    List<AudioDeviceInfo> GetAudioDevices();
    DeviceStatistics GetDeviceStatistics(List<AudioDeviceInfo> devices);
    List<AudioDeviceInfo> GetProblemDevices(List<AudioDeviceInfo> devices);
    List<string> GetAvailableDriverUpdates();
    bool ScanAndUpdateDrivers();
    (bool success, string message) RunWindowsUpdate();
    void OpenWindowsUpdateSettings();
}