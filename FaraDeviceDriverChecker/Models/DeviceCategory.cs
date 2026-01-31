namespace FaraDeviceDriverChecker.Models;

public class DeviceCategory
{
    public required string Name { get; set; }
    public required string[] Classes { get; set; }

    public static List<DeviceCategory> GetAll() =>
    [
        new() { Name = "オーディオ", Classes = ["Media", "AudioEndpoint"] },
        new() { Name = "ディスプレイ", Classes = ["Display"] },
        new() { Name = "ネットワーク", Classes = ["Net"] },
        new() { Name = "Bluetooth", Classes = ["Bluetooth"] },
        new() { Name = "USB", Classes = ["USB"] },
        new() { Name = "キーボード・マウス", Classes = ["Keyboard", "Mouse"] },
        new() { Name = "カメラ", Classes = ["Camera"] }
    ];
}
