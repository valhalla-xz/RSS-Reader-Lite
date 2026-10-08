namespace WinFormsApp1;

internal sealed class FeedOption
{
    public string Name { get; }
    public string Url { get; }
    public string Region { get; }
    public bool DefaultSelected { get; }
    public FeedOption(string name,string url,string region,bool selected){Name=name;Url=url;Region=region;DefaultSelected=selected;}
    public override string ToString()=>$"{Region}　|　{Name}　({Url})";
}

internal sealed class SetupWizardDialog : Form
{
    private static readonly FeedOption[] FeedOptions=
    [
        new("NHK ニュース", "https://www3.nhk.or.jp/rss/news/cat0.xml", "日本", true),
        new("ITmedia 総合", "https://rss.itmedia.co.jp/rss/2.0/itmedia_all.xml", "日本", true),
        new("ITmedia NEWS", "https://rss.itmedia.co.jp/rss/2.0/news_bursts.xml", "日本", false),
        new("ITmedia AI＋", "https://rss.itmedia.co.jp/rss/2.0/aiplus.xml", "日本", false),
        new("ITmedia Mobile", "https://rss.itmedia.co.jp/rss/2.0/mobile.xml", "日本", false),
        new("ITmedia PC USER", "https://rss.itmedia.co.jp/rss/2.0/pcuser.xml", "日本", false),
        new("ITmedia ビジネスオンライン", "https://rss.itmedia.co.jp/rss/2.0/business.xml", "日本", false),
        new("ITmedia エンタープライズ", "https://rss.itmedia.co.jp/rss/2.0/enterprise.xml", "日本", false),
        new("GIGAZINE", "https://gigazine.net/news/rss_2.0/", "日本", true),
        new("nippon.com ニュース", "https://www.nippon.com/ja/rss-others/news.xml", "日本", true),
        new("nippon.com 旅と暮らし", "https://www.nippon.com/ja/rss-others/guide-to-japan.xml", "日本", false),
        new("BBC World", "https://feeds.bbci.co.uk/news/world/rss.xml", "海外", true),
        new("BBC Asia", "https://feeds.bbci.co.uk/news/world/asia/rss.xml", "海外", false),
        new("BBC Business", "https://feeds.bbci.co.uk/news/business/rss.xml", "海外", true),
        new("BBC Politics", "https://feeds.bbci.co.uk/news/politics/rss.xml", "海外", false),
        new("BBC Technology", "https://feeds.bbci.co.uk/news/technology/rss.xml", "海外", false),
        new("BBC Science & Environment", "https://feeds.bbci.co.uk/news/science_and_environment/rss.xml", "海外", false),
        new("BBC Health", "https://feeds.bbci.co.uk/news/health/rss.xml", "海外", false),
        new("BBC Entertainment & Arts", "https://feeds.bbci.co.uk/news/entertainment_and_arts/rss.xml", "海外", false),
        new("BBC Sport", "https://feeds.bbci.co.uk/sport/rss.xml", "海外", false),
        new("The Guardian World", "https://www.theguardian.com/world/rss", "海外", true),
        new("The Guardian Business", "https://www.theguardian.com/business/rss", "海外", false),
        new("The Guardian Technology", "https://www.theguardian.com/technology/rss", "海外", false),
        new("The Guardian Science", "https://www.theguardian.com/science/rss", "海外", false),
        new("The Guardian Environment", "https://www.theguardian.com/environment/rss", "海外", false),
        new("The Guardian Culture", "https://www.theguardian.com/culture/rss", "海外", false),
        new("The Guardian Sport", "https://www.theguardian.com/sport/rss", "海外", false),
        new("Le Monde International", "https://www.lemonde.fr/en/international/rss_full.xml", "海外", true),
        new("Le Monde Economy", "https://www.lemonde.fr/en/economy/rss_full.xml", "海外", false),
        new("Le Monde Science", "https://www.lemonde.fr/en/science/rss_full.xml", "海外", false),
        new("Le Monde Health", "https://www.lemonde.fr/en/health/rss_full.xml", "海外", false),
        new("Le Monde Culture", "https://www.lemonde.fr/en/culture/rss_full.xml", "海外", false),
        new("Le Monde Sports", "https://www.lemonde.fr/en/sports/rss_full.xml", "海外", false),
        new("Le Monde Travel", "https://www.lemonde.fr/en/travel/rss_full.xml", "海外", false),
        new("NASA Breaking News", "https://www.nasa.gov/feed/", "海外", false),
    ];
    private static readonly SetupInterest[] InterestOptions=
    [
        new(){Name="国内ニュース",Terms=["日本","国内","Japan"],CategoryPaths=["国内ニュース"]},
        new(){Name="世界ニュース・国際情勢",Terms=["world","international","global","世界","国際"],CategoryPaths=["世界ニュース"]},
        new(){Name="政治・政策",Terms=["politics","government","election","policy","政治","選挙","政府"],CategoryPaths=["政治・社会"]},
        new(){Name="社会・事件",Terms=["society","community","crime","社会","事件","地域"],CategoryPaths=["政治・社会"]},
        new(){Name="経済・ビジネス",Terms=["business","economy","market","企業","経済","ビジネス"],CategoryPaths=["経済・ビジネス"]},
        new(){Name="テクノロジー",Terms=["technology","tech","digital","テクノロジー","デジタル"],CategoryPaths=["テクノロジー"]},
        new(){Name="AI・生成AI",Terms=["AI","artificial intelligence","machine learning","生成AI","人工知能"],CategoryPaths=["テクノロジー > AI"]},
        new(){Name="PC・半導体・GPU",Terms=["PC","CPU","GPU","Radeon","GeForce","semiconductor","半導体"],CategoryPaths=["PC・OS > ハードウェア"]},
        new(){Name="Windows・Linux",Terms=["Windows","Linux","Microsoft"],CategoryPaths=["PC・OS"]},
        new(){Name="スマートフォン・通信",Terms=["smartphone","mobile","telecom","スマートフォン","携帯電話","通信"],CategoryPaths=["テクノロジー > モバイル"]},
        new(){Name="自動車・EV",Terms=["automotive","EV","BEV","electric vehicle","自動車","電気自動車"],CategoryPaths=["自動車"]},
        new(){Name="モータースポーツ",Terms=["Formula 1","F1","motorsport","モータースポーツ"],CategoryPaths=["モータースポーツ"]},
        new(){Name="科学・宇宙",Terms=["science","NASA","space","科学","宇宙"],CategoryPaths=["科学・環境"]},
        new(){Name="環境・気候",Terms=["environment","climate","環境","気候","脱炭素"],CategoryPaths=["科学・環境"]},
        new(){Name="医療・健康",Terms=["health","medicine","medical","healthcare","医療","健康"],CategoryPaths=["医療・健康"]},
        new(){Name="教育・研究",Terms=["education","university","research","教育","大学","研究"],CategoryPaths=["教育・研究"]},
        new(){Name="文化・芸術",Terms=["culture","arts","art","文化","芸術"],CategoryPaths=["文化・エンタメ"]},
        new(){Name="映画・音楽・エンタメ",Terms=["film","movie","music","entertainment","映画","音楽","エンタメ"],CategoryPaths=["文化・エンタメ"]},
        new(){Name="スポーツ",Terms=["sport","football","baseball","tennis","スポーツ","野球","サッカー"],CategoryPaths=["スポーツ"]},
        new(){Name="ゲーム",Terms=["game","gaming","video game","ゲーム"],CategoryPaths=["ゲーム"]},
        new(){Name="旅行・観光",Terms=["travel","tourism","旅行","観光"],CategoryPaths=["暮らし・旅行"]},
        new(){Name="食・暮らし",Terms=["food","lifestyle","料理","食","暮らし"],CategoryPaths=["暮らし・旅行"]},
    ];
    private readonly TabControl _pages=new();
    private readonly CheckedListBox _feeds=new();
    private readonly CheckedListBox _interests=new();
    private readonly TextBox _customInterests=new();
    private readonly TextBox _customName=new();
    private readonly TextBox _customUrl=new();
    private readonly ComboBox _interval=new();
    private readonly Label _sourceCount=new();
    private readonly Label _summary=new();
    private readonly List<FeedOption> _customFeeds=new();

    public List<Feed> SelectedFeeds { get; private set; }=new();
    public List<SetupInterest> SelectedInterests { get; private set; }=new();
    public List<string> CustomInterests { get; private set; }=new();
    public int RefreshMinutes=>int.TryParse(_interval.SelectedItem as string,out var value)?value:10;

    public SetupWizardDialog()
    {
        Text="RSS Reader Lite へようこそ";Width=850;Height=690;MinimumSize=new Size(760,600);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.Sizable;
        _pages.Dock=DockStyle.Fill;_pages.TabPages.Add(BuildFeedPage());_pages.TabPages.Add(BuildInterestPage());_pages.TabPages.Add(BuildReviewPage());Controls.Add(_pages);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=50,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(8)};
        var finish=new Button{Text="ニュースを読み始める",AutoSize=true,DialogResult=DialogResult.None};finish.Click+=(_,_)=>FinishSetup();buttons.Controls.Add(finish);
        var next=new Button{Text="次へ",AutoSize=true};next.Click+=(_,_)=>{if(_pages.SelectedIndex<_pages.TabCount-1)_pages.SelectedIndex++;UpdateSummary();};buttons.Controls.Add(next);
        var back=new Button{Text="戻る",AutoSize=true};back.Click+=(_,_)=>{if(_pages.SelectedIndex>0)_pages.SelectedIndex--;};buttons.Controls.Add(back);
        var cancel=new Button{Text="終了",AutoSize=true,DialogResult=DialogResult.Cancel};buttons.Controls.Add(cancel);
        Controls.Add(buttons);CancelButton=cancel;_pages.SelectedIndexChanged+=(_,_)=>{next.Visible=_pages.SelectedIndex<2;finish.Visible=_pages.SelectedIndex==2;back.Enabled=_pages.SelectedIndex>0;UpdateSummary();};
        _feeds.ItemCheck+=(_,_)=>BeginInvoke(new Action(UpdateSourceCount));_interests.ItemCheck+=(_,_)=>BeginInvoke(new Action(UpdateSummary));UpdateSourceCount();
    }

    private TabPage BuildFeedPage()
    {
        var page=new TabPage("取得先");var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(18)};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,62));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,32));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,105));page.Controls.Add(layout);
        layout.Controls.Add(new Label{Text="日本・海外のニュース取得先を選んでください。RSS URLは後からフィード管理で変更できます。",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);
        _feeds.Dock=DockStyle.Fill;_feeds.CheckOnClick=true;foreach(var option in FeedOptions)_feeds.Items.Add(option,option.DefaultSelected);layout.Controls.Add(_feeds,0,1);
        _sourceCount.Dock=DockStyle.Fill;_sourceCount.TextAlign=ContentAlignment.MiddleLeft;layout.Controls.Add(_sourceCount,0,2);
        var custom=new GroupBox{Text="RSS/Atom URLを追加",Dock=DockStyle.Fill};var row=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(8),WrapContents=false};_customName.Width=150;_customName.Text="追加フィード";_customUrl.Width=410;_customUrl.Text="https://";row.Controls.Add(new Label{Text="名前",AutoSize=true,Padding=new Padding(0,7,0,0)});row.Controls.Add(_customName);row.Controls.Add(new Label{Text="URL",AutoSize=true,Padding=new Padding(0,7,0,0)});row.Controls.Add(_customUrl);var add=new Button{Text="追加",AutoSize=true};add.Click+=(_,_)=>AddCustomFeed();row.Controls.Add(add);custom.Controls.Add(row);layout.Controls.Add(custom,0,3);return page;
    }

    private TabPage BuildInterestPage()
    {
        var page=new TabPage("興味・関心");var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(18)};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));layout.RowStyles.Add(new RowStyle(SizeType.Percent,55));layout.RowStyles.Add(new RowStyle(SizeType.Percent,45));page.Controls.Add(layout);
        layout.Controls.Add(new Label{Text="興味のあるカテゴリを選択してください。おすすめ記事の順位付けに使います。",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);
        _interests.Dock=DockStyle.Fill;_interests.CheckOnClick=true;foreach(var option in InterestOptions)_interests.Items.Add(option,option.Name is "国内ニュース" or "世界ニュース・国際情勢" or "経済・ビジネス" or "テクノロジー" or "文化・芸術" or "スポーツ");layout.Controls.Add(_interests,0,1);
        var custom=new GroupBox{Text="追加のキーワード（1行に1つ）",Dock=DockStyle.Fill};_customInterests.Dock=DockStyle.Fill;custom.Controls.Add(_customInterests);layout.Controls.Add(custom,0,2);return page;
    }

    private TabPage BuildReviewPage()
    {
        var page=new TabPage("確認");var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(18)};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,55));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,45));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));page.Controls.Add(layout);
        layout.Controls.Add(new Label{Text="アプリ起動中の自動更新間隔",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);_interval.DropDownStyle=ComboBoxStyle.DropDownList;_interval.Items.AddRange(["0","5","10","15","30","60"]);_interval.SelectedItem="10";_interval.Width=160;layout.Controls.Add(_interval,0,1);
        _summary.Dock=DockStyle.Fill;_summary.TextAlign=ContentAlignment.TopLeft;layout.Controls.Add(_summary,0,2);return page;
    }

    private void AddCustomFeed()
    {
        var name=_customName.Text.Trim();var url=_customUrl.Text.Trim();if(string.IsNullOrWhiteSpace(name)){MessageBox.Show(this,"フィード名を入力してください。","入力の確認");return;}if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme!="https"&&uri.Scheme!="http"){MessageBox.Show(this,"http:// または https:// で始まる有効なURLを入力してください。","入力の確認");return;}if(_customFeeds.Any(x=>string.Equals(x.Url,url,StringComparison.OrdinalIgnoreCase))||FeedOptions.Any(x=>string.Equals(x.Url,url,StringComparison.OrdinalIgnoreCase))){MessageBox.Show(this,"同じURLはすでに登録候補にあります。","入力の確認");return;}
        var option=new FeedOption(name,url,"追加",true);_customFeeds.Add(option);_feeds.Items.Add(option,true);_customUrl.Text="https://";UpdateSourceCount();
    }

    private void UpdateSourceCount(){if(_sourceCount.IsDisposed)return;_sourceCount.Text=$"選択中：{_feeds.CheckedItems.Count} 件";UpdateSummary();}
    private void UpdateSummary(){if(_summary.IsDisposed)return;_summary.Text=$"取得先：{_feeds.CheckedItems.Count} 件\r\n興味カテゴリ：{_interests.CheckedItems.Count} 件\r\n追加キーワード：{_customInterests.Lines.Count(x=>!string.IsNullOrWhiteSpace(x))} 件\r\n\r\n取得処理は選択したフィードごとに行われ、失敗したフィードがあっても他の取得先は継続します。";}

    private void FinishSetup()
    {
        var feeds=_feeds.CheckedItems.Cast<FeedOption>().ToList();if(feeds.Count==0){MessageBox.Show(this,"最低1つのニュース取得先を選択してください。","取得先の確認");_pages.SelectedIndex=0;return;}
        SelectedFeeds=feeds.Select(x=>new Feed{Name=x.Name,Url=x.Url,Enabled=true}).ToList();SelectedInterests=_interests.CheckedItems.Cast<SetupInterest>().ToList();CustomInterests=_customInterests.Lines.Select(x=>x.Trim()).Where(x=>x.Length>0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();DialogResult=DialogResult.OK;Close();
    }
}
