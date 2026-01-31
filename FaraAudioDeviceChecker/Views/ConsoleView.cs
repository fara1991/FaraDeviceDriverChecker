namespace FaraAudioDeviceChecker.Views;

using Models;

public class ConsoleView
{
    public static void ShowHeader()
    {
        Console.WriteLine("=== Audioドライバー状態 ===\n");
    }

    public static void ShowDeviceCount(int count)
    {
        Console.WriteLine($"検出されたAudioデバイス数: {count}\n");
    }

    public static void ShowNoDevicesFound()
    {
        Console.WriteLine("Audioデバイスが見つかりませんでした。");
    }

    public static void ShowDeviceAnalysis(AudioDeviceInfo device)
    {
        Console.WriteLine($"デバイス名: {device.Name}");
        Console.WriteLine($"製造元: {device.Manufacturer}");
        Console.WriteLine($"デバイスID: {device.DeviceId}");
        Console.WriteLine($"ドライバーバージョン: {device.DriverVersion}");
        Console.WriteLine($"ドライバー日付: {device.DriverDate}");
        Console.WriteLine($"ステータス: {device.Status}");
        Console.WriteLine($"クラス: {device.Class}");
        Console.WriteLine($"サービス: {device.Service}");

        var needsAttention = false;
        var issues = new List<string>();

        if (device.HasProblem)
        {
            issues.Add($"問題コード: {device.ProblemCode}");
            needsAttention = true;
        }

        if (device.Status != "OK")
        {
            issues.Add($"ステータスが正常ではありません: {device.Status}");
            needsAttention = true;
        }

        if (device.DriverVersion.StartsWith("取得エラー:"))
        {
            issues.Add("ドライバー情報が取得できませんでした");
            needsAttention = true;
        }

        if (needsAttention)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n⚠️ 注意が必要:");
            foreach (var issue in issues)
            {
                Console.WriteLine($"  - {issue}");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✅ 正常に動作しています");
        }

        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
    }

    public static void ShowProblemSummary(List<AudioDeviceInfo> problemDevices)
    {
        Console.WriteLine("\n=== 問題のあるデバイス要約 ===");

        if (problemDevices.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("すべてのAudioデバイスが正常に動作しています。");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{problemDevices.Count}個のデバイスに問題があります:");
            Console.ResetColor();

            foreach (var device in problemDevices)
            {
                var reason = device.DriverVersion.StartsWith("取得エラー:") ? "ドライバー情報取得エラー" : device.ProblemCode;
                Console.WriteLine($"  - {device.Name}: {reason}");
            }
        }
    }

    public static void ShowCheckingForUpdates()
    {
        Console.WriteLine("\n利用可能なドライバー更新を確認中...");
    }

    public static void ShowDeviceStatistics(DeviceStatistics statistics)
    {
        Console.WriteLine("\n=== デバイス統計 ===");

        Console.WriteLine("\nデバイスクラス別統計:");
        foreach (var kvp in statistics.ClassCount)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}個");
        }

        Console.WriteLine("\nステータス別統計:");
        foreach (var kvp in statistics.StatusCount)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}個");
        }

        Console.WriteLine("\n製造元別統計:");
        foreach (var kvp in statistics.ManufacturerCount)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}個");
        }
    }

    public static void ShowRecommendations(List<AudioDeviceInfo> problemDevices, List<string> availableUpdates)
    {
        Console.WriteLine("\n=== 推奨事項 ===");

        if (problemDevices.Count == 0 && availableUpdates.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("現在、特に対応が必要な問題は見つかりませんでした。");
            Console.ResetColor();
            return;
        }

        if (problemDevices.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"🔴 緊急対応が必要: {problemDevices.Count}個のデバイス");
            Console.ResetColor();
            Console.WriteLine("以下の方法で対応してください:");
            Console.WriteLine("  1. デバイスマネージャーを開く");
            Console.WriteLine("  2. 問題のあるデバイスを右クリック");
            Console.WriteLine("  3. 「ドライバーの更新」を選択");
            Console.WriteLine("  4. 「ドライバーを自動的に検索」でうまくいかない場合、「コンピュータを参照してドライバーを検索」を選択");
            Console.WriteLine("  5. 「次の場所でドライバーを検索します」の入力欄で C:\\Windowsに変更して「次へ」を選択");
            Console.WriteLine();
        }

        if (availableUpdates.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"🟡 利用可能なドライバー更新: {availableUpdates.Count}件");
            Console.ResetColor();
            foreach (var update in availableUpdates)
            {
                Console.WriteLine($"  - {update}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("📋 一般的な対応手順:");
        Console.WriteLine("  • デバイスマネージャー: Windowsキー + X → デバイスマネージャー");
        Console.WriteLine("  • Windows Update: 設定 → Windows Update → 更新プログラムのチェック");
        Console.WriteLine("  • 製造元サイト: 各デバイスの製造元の公式サポートページ");
    }

    public static void ShowError(string message)
    {
        Console.WriteLine($"エラーが発生しました: {message}");
    }

    public static int ShowUpdateMenu()
    {
        Console.WriteLine("\n=== ドライバー更新オプション ===");
        Console.WriteLine("1. Windows Updateでドライバーを検索・更新");
        Console.WriteLine("2. Windows Updateの設定画面を開く（オプションの更新プログラム）");
        Console.WriteLine("3. デバイススキャン（pnputil）");
        Console.WriteLine("0. 何もしない");
        Console.Write("\n選択してください (0-3): ");

        var input = Console.ReadLine()?.Trim();
        return int.TryParse(input, out var choice) ? choice : 0;
    }

    public static void ShowWindowsUpdateInProgress()
    {
        Console.WriteLine("\nWindows Updateを実行中...");
        Console.WriteLine("※スキャン・ダウンロード・インストールが完了するまで待機します。");
        Console.WriteLine("※数分〜数十分かかる場合があります。しばらくお待ちください...");
    }

    public static void ShowWindowsUpdateResult(bool success, string message)
    {
        Console.WriteLine();
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.WriteLine("ドライバーが更新された場合、再起動が必要な場合があります。");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
        }
        Console.ResetColor();
    }

    public static void ShowOpeningSettings()
    {
        Console.WriteLine("\nWindows Updateの設定画面を開いています...");
        Console.WriteLine("「オプションの更新プログラム」からドライバー更新を選択してください。");
    }

    public static void ShowDeviceScanInProgress()
    {
        Console.WriteLine("\nデバイススキャンを実行中...");
    }

    public static void ShowDeviceScanResult(bool success)
    {
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("デバイススキャンが完了しました。");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("デバイススキャンに失敗しました。管理者権限で実行してください。");
        }
        Console.ResetColor();
    }

    public static void ShowExitPrompt()
    {
        Console.WriteLine("\nEnterキーを押して終了してください...");
        Console.ReadLine();
    }
}