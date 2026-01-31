# FaraDeviceDriverChecker

Windowsのデバイスドライバーの状態を診断し、必要に応じてWindows Updateを通じて更新をサポートするGUIツールです。

## 技術スタック

- **.NET 9.0** (WPF)
- **Material Design In XAML Toolkit** - モダンなUI
- **WMI (Windows Management Instrumentation)** - デバイス情報の取得
- **Windows Update API** - ドライバー更新の検出・インストール

## 主な機能

- **マルチカテゴリ診断**: 以下の主要なデバイスカテゴリを個別にスキャン
  - オーディオ
  - ディスプレイ
  - ネットワーク
  - Bluetooth
  - USB
  - キーボード・マウス
  - カメラ
- **デバイス状態の可視化**: デバイスマネージャーのステータスに基づき、正常、注意、エラーの状態を分かりやすく表示。
- **ドライバー更新**: Windows Update APIを利用して、ドライバー更新プログラムをインストール。
- **Windows Update連携**: アプリケーション内から直接Windows Updateの設定を開いたり、更新を試行したりすることが可能。

## インストール

インストールは不要です。

1. [最新リリース](https://github.com/fara1991/FaraDeviceChecker/releases)から `FaraDeviceDriverChecker-vX.X.X-win-x64.zip` をダウンロード
2. ZIPファイルを解凍
3. `FaraDeviceDriverChecker.exe` を実行

## 使い方

1. **カテゴリの選択**: 画面上部のドロップダウンから、診断したいデバイスカテゴリ（例：オーディオ）を選択します。
2. **スキャンの実行**: 「スキャン」ボタンをクリックすると、選択したカテゴリのデバイス一覧と状態が表示されます。
3. **更新の実行**: 問題があるデバイスがある場合、「Updateを実行」ボタンからドライバーの更新をインストールできます。
4. **設定へのアクセス**: 「設定を開く」ボタンから、システムのWindows Update設定を直接開くことができます。

## 結果の見方

各デバイスのステータスに応じて、以下のアイコンが表示されます：

| アイコン | 状態 | 説明 |
|:---:|:---:|:---|
| 緑のチェックマーク | 正常 | デバイスは正しく認識され、問題なく動作しています |
| 赤の警告マーク | 問題あり | デバイスに問題が発生しています。詳細列に具体的なエラー内容が表示されます |

## システム要件

- **OS**: Windows 10 以降 (64bit)
- **ランタイム**: 不要 (リリース版は単一実行ファイルとして配布)
- **権限**: ドライバー更新を実行する場合は、管理者権限が必要です

## トラブルシューティング

### プログラムが起動しない
- セキュリティソフトによってブロックされている場合は、実行を許可してください。
- Windows SmartScreen が警告を表示する場合は、「詳細情報」から実行を許可できます。

### デバイスが表示されない
- デバイスが物理的に接続されているか確認してください。
- デバイスマネージャーでデバイスが認識されているか確認してください。

### ドライバー更新が失敗する
- 管理者権限でアプリケーションを実行してください。
- Windows Updateサービスが動作しているか確認してください。

## 注意事項

- このツールはデバイスの状態を確認し、標準的なWindows Updateの機能を呼び出すものです。
- 診断結果に基づいてドライバーを更新する際は、事前にシステムの復元ポイントを作成することを推奨します。
- 企業環境では、IT管理者に相談してからドライバー更新を行ってください。

## プロジェクト構成

```
FaraDeviceDriverChecker/
├── Models/
│   ├── DeviceInfo.cs        # デバイス情報モデル
│   └── DeviceCategory.cs    # デバイスカテゴリ定義
├── ViewModels/
│   └── MainViewModel.cs     # メインビューモデル (MVVM)
├── Services/
│   └── DeviceService.cs     # デバイス情報取得・更新サービス
├── Utilities/
│   ├── WmiHelper.cs         # WMIクエリヘルパー
│   └── ErrorCodeHelper.cs   # エラーコード変換
├── MainWindow.xaml          # メインウィンドウUI
└── App.xaml                 # アプリケーション設定
```

## ビルド方法

### 必要条件

- .NET 9.0 SDK
- Windows 10 以降

### ビルド手順

```bash
# リポジトリをクローン
git clone https://github.com/fara1991/FaraDeviceChecker.git
cd FaraDeviceChecker

# 依存関係の復元
dotnet restore

# デバッグビルド
dotnet build

# リリースビルド (単一実行ファイル)
dotnet publish FaraDeviceDriverChecker/FaraDeviceDriverChecker.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
```

## サポート

問題や質問がある場合は、[GitHubのIssues](https://github.com/fara1991/FaraDeviceChecker/issues)で報告してください。
報告時には、Windowsのバージョンと発生している現象の詳細を添えていただけますと幸いです。

## ライセンス

MIT License