namespace FaraAudioDeviceChecker.Controllers;

using Services;
using Views;

public class AudioDeviceController(IDeviceService deviceService)
{
    public void Run()
    {
        try
        {
            // デバイスカテゴリを選択
            var (deviceClasses, categoryName) = ConsoleView.ShowDeviceCategoryMenu();

            ConsoleView.ShowHeader(categoryName);

            var devices = deviceService.GetDevices(deviceClasses);

            if (devices.Count == 0)
            {
                ConsoleView.ShowNoDevicesFound(categoryName);
                return;
            }

            ConsoleView.ShowDeviceCount(devices.Count, categoryName);

            // 各デバイスの分析を表示
            foreach (var device in devices)
            {
                ConsoleView.ShowDeviceAnalysis(device);
            }

            // 問題のあるデバイスの要約
            var problemDevices = deviceService.GetProblemDevices(devices);
            ConsoleView.ShowProblemSummary(problemDevices);

            // 統計情報の表示
            var statistics = deviceService.GetDeviceStatistics(devices);
            ConsoleView.ShowDeviceStatistics(statistics);

            // 利用可能なドライバー更新をチェック
            ConsoleView.ShowCheckingForUpdates();
            var availableUpdates = deviceService.GetAvailableDriverUpdates();

            // 推奨事項の表示
            ConsoleView.ShowRecommendations(problemDevices, availableUpdates);

            // 問題のあるデバイスまたは利用可能な更新がある場合、更新を提案
            if (problemDevices.Count > 0 || availableUpdates.Count > 0)
            {
                var choice = ConsoleView.ShowUpdateMenu();
                switch (choice)
                {
                    case 1:
                        ConsoleView.ShowWindowsUpdateInProgress();
                        var (updateSuccess, updateMessage) = deviceService.RunWindowsUpdate();
                        ConsoleView.ShowWindowsUpdateResult(updateSuccess, updateMessage);
                        break;
                    case 2:
                        ConsoleView.ShowOpeningSettings();
                        deviceService.OpenWindowsUpdateSettings();
                        break;
                    case 3:
                        ConsoleView.ShowDeviceScanInProgress();
                        var scanSuccess = deviceService.ScanAndUpdateDrivers();
                        ConsoleView.ShowDeviceScanResult(scanSuccess);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleView.ShowError(ex.Message);
        }
        finally
        {
            ConsoleView.ShowExitPrompt();
        }
    }
}