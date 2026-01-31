namespace FaraAudioDeviceChecker.Services;

using System.Diagnostics;
using System.Management;
using Models;
using Utilities;

public class DeviceService : IDeviceService
{
    public List<AudioDeviceInfo> GetDevices(string[] deviceClasses)
    {
        var devices = new List<AudioDeviceInfo>();

        var deviceIds = GetDeviceIdsByClasses(deviceClasses);

        foreach (var deviceId in deviceIds)
        {
            var deviceInfo = GetDeviceInfoById(deviceId);
            if (deviceInfo != null)
            {
                devices.Add(deviceInfo);
            }
        }

        return RemoveDuplicates(devices);
    }

    private List<string> GetDeviceIdsByClasses(string[] deviceClasses)
    {
        var deviceIds = new List<string>();

        foreach (var deviceClass in deviceClasses)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "pnputil",
                    Arguments = $"/enum-devices /class {deviceClass}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null) continue;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // "インスタンス ID:" または "Instance ID:" の行からデバイスIDを抽出
                var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Contains("インスタンス ID:") || line.Contains("Instance ID:"))
                    {
                        var id = line.Split(':').LastOrDefault()?.Trim();
                        if (!string.IsNullOrEmpty(id))
                        {
                            deviceIds.Add(id);
                        }
                    }
                }
            }
            catch
            {
                // エラー時は継続
            }
        }

        return deviceIds;
    }

    private AudioDeviceInfo? GetDeviceInfoById(string deviceId)
    {
        try
        {
            var escapedDeviceId = WmiHelper.EscapeWqlString(deviceId);
            var query = $"SELECT * FROM Win32_PnPEntity WHERE DeviceID = '{escapedDeviceId}'";

            using var searcher = new ManagementObjectSearcher(query);
            var collection = searcher.Get();

            foreach (var o in collection)
            {
                var device = (ManagementObject)o;
                var deviceInfo = new AudioDeviceInfo
                {
                    Name = WmiHelper.GetPropertyValue(device, "Name"),
                    DeviceId = WmiHelper.GetPropertyValue(device, "DeviceID"),
                    Description = WmiHelper.GetPropertyValue(device, "Description"),
                    Manufacturer = WmiHelper.GetPropertyValue(device, "Manufacturer"),
                    Status = WmiHelper.GetPropertyValue(device, "Status"),
                    Class = WmiHelper.GetPropertyValue(device, "PNPClass"),
                    Service = WmiHelper.GetPropertyValue(device, "Service"),
                    HardwareId = WmiHelper.GetPropertyValue(device, "HardwareID")
                };

                SetProblemCode(device, deviceInfo);
                GetDriverInfo(deviceInfo);
                return deviceInfo;
            }
        }
        catch
        {
            // エラー時はnullを返す
        }

        return null;
    }

    public DeviceStatistics GetDeviceStatistics(List<AudioDeviceInfo> devices)
    {
        var statistics = new DeviceStatistics();

        foreach (var device in devices)
        {
            var className = string.IsNullOrEmpty(device.Class) ? "不明" : device.Class;
            if (!statistics.ClassCount.TryAdd(className, 1))
                statistics.ClassCount[className]++;

            var status = string.IsNullOrEmpty(device.Status) ? "不明" : device.Status;
            if (!statistics.StatusCount.TryAdd(status, 1))
                statistics.StatusCount[status]++;

            var manufacturer = string.IsNullOrEmpty(device.Manufacturer) ? "不明" : device.Manufacturer;
            if (!statistics.ManufacturerCount.TryAdd(manufacturer, 1))
                statistics.ManufacturerCount[manufacturer]++;
        }

        return statistics;
    }

    public List<AudioDeviceInfo> GetProblemDevices(List<AudioDeviceInfo> devices)
    {
        return devices.FindAll(d => d.HasProblem || d.Status != "OK" || d.DriverVersion.StartsWith("取得エラー:"));
    }

    public List<string> GetAvailableDriverUpdates()
    {
        var updates = new List<string>();

        try
        {
            var script = @"
                $UpdateSession = New-Object -ComObject Microsoft.Update.Session
                $UpdateSearcher = $UpdateSession.CreateUpdateSearcher()
                $SearchResult = $UpdateSearcher.Search('IsInstalled=0 and Type=''Driver''')
                foreach ($Update in $SearchResult.Updates) {
                    Write-Output $Update.Title
                }
            ";

            var processInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                updates.AddRange(lines);
            }
        }
        catch
        {
            // エラー時は空リストを返す
        }

        return updates;
    }

    public bool ScanAndUpdateDrivers()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "pnputil",
                Arguments = "/scan-devices",
                UseShellExecute = true,
                Verb = "runas"
            };

            using var process = Process.Start(processInfo);
            process?.WaitForExit();

            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public (bool success, string message) RunWindowsUpdate()
    {
        try
        {
            // PowerShellでWindows Updateを実行
            var script = @"
                $UpdateSession = New-Object -ComObject Microsoft.Update.Session
                $UpdateSearcher = $UpdateSession.CreateUpdateSearcher()
                Write-Host 'ドライバー更新を検索中...'
                $SearchResult = $UpdateSearcher.Search('IsInstalled=0 and Type=''Driver''')

                if ($SearchResult.Updates.Count -eq 0) {
                    Write-Host '利用可能なドライバー更新はありません。'
                    exit 0
                }

                Write-Host ""$($SearchResult.Updates.Count) 件のドライバー更新が見つかりました。""

                $UpdatesToInstall = New-Object -ComObject Microsoft.Update.UpdateColl
                foreach ($Update in $SearchResult.Updates) {
                    Write-Host ""  - $($Update.Title)""
                    $UpdatesToInstall.Add($Update) | Out-Null
                }

                Write-Host 'ダウンロード中...'
                $Downloader = $UpdateSession.CreateUpdateDownloader()
                $Downloader.Updates = $UpdatesToInstall
                $Downloader.Download() | Out-Null

                Write-Host 'インストール中...'
                $Installer = $UpdateSession.CreateUpdateInstaller()
                $Installer.Updates = $UpdatesToInstall
                $InstallResult = $Installer.Install()

                if ($InstallResult.RebootRequired) {
                    Write-Host '再起動が必要です。'
                }

                Write-Host '完了しました。'
                exit 0
            ";

            var processInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false,
                Verb = "runas"
            };

            // 管理者権限で実行するため、UseShellExecute=trueに変更
            processInfo.UseShellExecute = true;
            processInfo.RedirectStandardOutput = false;
            processInfo.RedirectStandardError = false;
            processInfo.Verb = "runas";

            using var process = Process.Start(processInfo);
            process?.WaitForExit();

            return process?.ExitCode == 0
                ? (true, "Windows Updateが完了しました。")
                : (false, "Windows Updateの実行中にエラーが発生しました。");
        }
        catch (Exception ex)
        {
            return (false, $"エラー: {ex.Message}");
        }
    }

    public void OpenWindowsUpdateSettings()
    {
        try
        {
            // オプションの更新プログラム画面を開く
            var processInfo = new ProcessStartInfo
            {
                FileName = "ms-settings:windowsupdate-optionalupdates",
                UseShellExecute = true
            };

            Process.Start(processInfo);
        }
        catch
        {
            // フォールバック: 通常のWindows Update画面を開く
            var processInfo = new ProcessStartInfo
            {
                FileName = "ms-settings:windowsupdate",
                UseShellExecute = true
            };

            Process.Start(processInfo);
        }
    }

    private static void SetProblemCode(ManagementObject device, AudioDeviceInfo deviceInfo)
    {
        if (device["ConfigManagerErrorCode"] == null) return;
        
        var problemCode = (uint) device["ConfigManagerErrorCode"];
        deviceInfo.HasProblem = problemCode != 0;
        deviceInfo.ProblemCode = ErrorCodeHelper.GetProblemDescription(problemCode);
    }

    private static void GetDriverInfo(AudioDeviceInfo device)
    {
        try
        {
            var escapedDeviceId = WmiHelper.EscapeWqlString(device.DeviceId);

            var query = $"SELECT * FROM Win32_PnPSignedDriver WHERE DeviceID = '{escapedDeviceId}'";
            using var searcher = new ManagementObjectSearcher(query);
            var collection = searcher.Get();

            foreach (var o in collection)
            {
                var driver = (ManagementObject) o;
                device.DriverVersion = driver["DriverVersion"]?.ToString() ?? "不明";

                if (driver["DriverDate"] != null)
                {
                    var driverDate = ManagementDateTimeConverter.ToDateTime(driver["DriverDate"].ToString());
                    device.DriverDate = driverDate.ToString("yyyy/MM/dd");
                }

                break;
            }

            if (device.DriverVersion == "不明" && !string.IsNullOrEmpty(device.Service))
            {
                GetSystemDriverInfo(device);
            }
        }
        catch (Exception ex)
        {
            device.DriverVersion = $"取得エラー: {ex.Message}";
            device.HasProblem = true;
            device.ProblemCode = "ドライバー情報取得エラー";
        }
    }

    private static void GetSystemDriverInfo(AudioDeviceInfo device)
    {
        var escapedService = WmiHelper.EscapeWqlString(device.Service);
        var query = $"SELECT * FROM Win32_SystemDriver WHERE Name = '{escapedService}'";
        using var driverSearcher = new ManagementObjectSearcher(query);
        var driverCollection = driverSearcher.Get();

        foreach (var o in driverCollection)
        {
            var driver = (ManagementObject) o;
            device.DriverVersion = driver["Version"]?.ToString() ?? "不明";

            if (driver["InstallDate"] != null)
            {
                var installDate = ManagementDateTimeConverter.ToDateTime(driver["InstallDate"].ToString());
                device.DriverDate = installDate.ToString("yyyy/MM/dd");
            }

            break;
        }
    }

    private static List<AudioDeviceInfo> RemoveDuplicates(List<AudioDeviceInfo> devices)
    {
        var seenDeviceIds = new HashSet<string>();
        return devices.Where(device => seenDeviceIds.Add(device.DeviceId)).ToList();
    }
}