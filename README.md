# ローカルニュースリーダー

RSS/Atomニュースを端末内のSQLiteへ保存し、分類ルールや興味設定で整理して閲覧するWindowsデスクトップアプリです。常駐サービス、タスクトレイ、スタートアップ登録は行いません。自動更新はアプリを開いている間だけ動作します。

## 対応環境と技術選定

- Windows 10 22H2 x64、およびWindows 11 x64
- .NET Framework 4.8 / Windows Forms
- RSS/Atom解析: `System.ServiceModel.Syndication`
- ローカル保存: `Microsoft.Data.Sqlite` と `SQLitePCLRaw`
- 内蔵記事表示: Microsoft Edge WebView2 Runtime

Windows 10 22H2には.NET Framework 4.8が含まれます。現行.NET 8/10のWindows 10対応はEnterprise/LTSCなどに限られるため、このアプリは22H2で動く.NET Framework 4.8を対象にします。Windows 10 Home/Pro 22H2自体は2025年10月にサポート終了しているため、OSを利用する場合はMicrosoftのESUまたはWindows 11への移行をご検討ください。[.NET Frameworkの対応OS](https://learn.microsoft.com/en-us/dotnet/framework/get-started/system-requirements) / [Windows 10 22H2のサポート終了](https://learn.microsoft.com/en-us/lifecycle/announcements/windows-10-22h2-end-of-support-update)

WebView2 Evergreen RuntimeはWindows 10/11で利用でき、Windows 11には通常プリインストールされています。Windows 10では未導入の端末があるため、記事をアプリ内で読むには[WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)をインストールしてください。外部ブラウザで開く機能はWebView2がなくても使えます。

## 初回セットアップ

初回起動時に3画面のウィザードが開きます。

1. 日本・海外のおすすめRSS/Atomフィードを選択し、任意URLも追加
2. 興味カテゴリを選び、独自キーワードを入力
3. 自動更新間隔を確認して開始

初期フィードにはNHK、ITmedia NEWS/AI+/PC USER、GIGAZINE、BBC World/Technology/Science、The Guardian World、NASAを用意しています。ITmedia RSSは[運営元のRSS一覧](https://corp.itmedia.co.jp/media/rss_list/)、GIGAZINEは[公式のRSS URL案内](https://gigazine.net/news/20180620-gigazine-rss-change/)を参照しています。配信元の仕様や公開状況は変更されるため、取得結果はフィード管理画面で確認してください。

## ビルドと起動

Visual Studio Community 2026の.NETデスクトップ開発機能、または.NET 10 SDKと.NET Framework 4.8 Developer Packが必要です。初回はNuGet.orgへの接続が必要です。

```powershell
dotnet restore .\WinFormsApp1.slnx
dotnet build .\WinFormsApp1.slnx -c Release
dotnet run --project .\WinFormsApp1\WinFormsApp1.csproj
```

Windows向けx64アプリとしてビルドされます。配布用出力は次で作成できます。

```powershell
dotnet publish .\WinFormsApp1\WinFormsApp1.csproj -c Release -o .\artifacts\publish
```

## 使用ライブラリ

| NuGet | 用途 |
|---|---|
| Microsoft.Data.Sqlite 8.0.22 | SQLiteアクセス |
| SQLitePCLRaw.lib.e_sqlite3 2.1.13 | SQLiteネイティブエンジン |
| System.ServiceModel.Syndication 8.0.0 | RSS/Atom解析 |
| Microsoft.Web.WebView2 1.0.4258.31 | 内蔵ブラウザ |

## 保存先と更新

設定、フィード、記事、既読状態、カテゴリ、分類ルール、興味キーワードは`%LOCALAPPDATA%\LocalNewsReader\news.db`に保存します。更新間隔は手動のみ、5、10、15、30、60分から選べます。アプリを閉じると自動更新と進行中のRSS取得を停止します。

## 主な機能

- フィードの追加・編集・有効/無効・お気に入り・削除・取得テスト
- RSS/Atom取得、GUIDによる重複抑制、失敗フィードの個別記録
- ホーム、おすすめ、新着、未読、お気に入りメディア、カテゴリ、フィード別表示と検索
- 階層カテゴリと分類ルール（タイトル/本文、AND/OR/NOT、正規表現、優先順位）
- 興味カテゴリとキーワードに基づくローカルおすすめ
- 記事の既読状態、WebView2内蔵表示、既定ブラウザ表示

## テスト

アプリのビルドに加え、ウィザードのGUIイベント、入力検証、SQLite保存、RSS/Atom取得、重複防止、不正XMLのフィード単位処理を確認するスモークテストを用意しています。

```powershell
dotnet restore .\tests\LocalNewsReader.SmokeTests\LocalNewsReader.SmokeTests.csproj
dotnet run --project .\tests\LocalNewsReader.SmokeTests\LocalNewsReader.SmokeTests.csproj
```

テストは一時DBとローカルHTTP RSS fixtureを使い、実際のフィード登録や利用者のデータは変更しません。
