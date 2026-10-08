# RSS Reader Lite

RSS/Atomニュースを端末内のSQLiteへ保存し、分類ルールや興味設定で整理して閲覧するWindowsデスクトップアプリです。常駐サービス、タスクトレイ、スタートアップ登録は行いません。自動更新はアプリを開いている間だけ動作します。

GitHub: https://github.com/valhalla-xz/RSS-Reader-Lite

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

初期フィード候補は日本・海外の複数分野から選べます。日本はNHK、ITmedia総合/NEWS/AI+/Mobile/PC USER/ビジネス/エンタープライズ、GIGAZINE、nippon.comニュース/旅と暮らし。海外はBBC（世界、アジア、政治、経済、テクノロジー、科学、健康、芸術、スポーツ）、The Guardian（世界、経済、テクノロジー、科学、環境、文化、スポーツ）、Le Monde（国際、経済、科学、健康、文化、スポーツ、旅行）、NASAを用意しています。既定では国内一般、海外一般、経済などを選択し、個別ジャンルはウィザードで任意に追加できます。ITmediaの提供カテゴリは[運営元のRSS一覧](https://corp.itmedia.co.jp/media/rss_list/)、BBCは[公式フィード案内](https://support.bbc.co.uk/platform/feeds/NewsFeeds.htm)、Guardianは[公式RSS説明](https://www.theguardian.com/help/feeds)、Le Mondeは[公式RSS一覧](https://www.lemonde.fr/en/about-us/article/2026/03/27/le-monde-rss-feeds_6751860_115.html)、nippon.comは[公式RSS一覧](https://www.nippon.com/ja/rss_list/)を参照しています。GIGAZINEは[公式RSS URL案内](https://gigazine.net/news/20180620-gigazine-rss-change/)を参照しています。配信元の仕様やURLは変更されるため、初回取得結果はフィード管理画面でご確認ください。

初期カテゴリと興味の候補も、国内外ニュース、政治・社会、経済、テクノロジー、PC/モバイル、自動車、科学、環境、健康、教育、文化、スポーツ、ゲーム、旅行・食を含む構成です。初期選択は広めのジャンルに設定し、個人の関心に合わせて後から追加・変更できます。

## 開発・ビルド・GitHubへの反映

Visual Studio Community 2026の.NETデスクトップ開発機能、または.NET 10 SDKと.NET Framework 4.8 Developer Packが必要です。初回はNuGet.orgへの接続が必要です。

```powershell
git clone https://github.com/valhalla-xz/RSS-Reader-Lite.git
cd RSS-Reader-Lite
dotnet restore .\WinFormsApp1.slnx
dotnet build .\WinFormsApp1.slnx -c Release
dotnet run --project .\WinFormsApp1\WinFormsApp1.csproj
```

既存チェックアウトでリモートURLを確認・修正する場合:

```powershell
git remote -v
git remote set-url origin https://github.com/valhalla-xz/RSS-Reader-Lite.git
git fetch origin
```

変更をコミットして現在のブランチをGitHubへプッシュする場合:

```powershell
git add README.md CHANGELOG.md WinFormsApp1 tests
git commit -m "変更内容を短く記載"
git push origin HEAD
```

GitHub Actionsが有効な場合、重複しないバージョンタグをプッシュするとWindowsビルドとスモークテストを実行し、成功後に配布ZIPを添付したGitHub Releaseを公開します。Actionsが無効な場合は、作成済みのZIPを使いGitHubの[Releases](https://github.com/valhalla-xz/RSS-Reader-Lite/releases)から手動でリリースしてください。このv0.1.2はActionsの実行履歴が確認できなかったため、GitHub APIからリリースを作成しています。

```powershell
git tag v0.1.4
git push origin v0.1.4
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

設定、フィード、記事、既読状態、カテゴリ、分類ルール、興味キーワードは`%LOCALAPPDATA%\LocalNewsReader\news.db`に保存します。起動時エラーの詳細は`%LOCALAPPDATA%\LocalNewsReader\startup.log`に記録します。更新間隔は手動のみ、5、10、15、30、60分から選べます。アプリを閉じると自動更新と進行中のRSS取得を停止します。

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
