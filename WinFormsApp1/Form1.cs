using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WinFormsApp1;

public partial class Form1 : Form
{
    private readonly NewsStore _store;
    private readonly FeedReader _reader;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly ListBox _navigation = new();
    private readonly DataGridView _articles = new();
    private readonly WebView2 _browser = new();
    private readonly TextBox _search = new();
    private readonly Label _status = new();
    private readonly ComboBox _interval = new();
    private readonly Button _refresh = new();
    private string _view = "ホーム";
    private bool _loading;
    private bool _needsSetup;
    private bool _shownInitializationStarted;
    private long? _selectedCategoryId;

    public Form1() : this(null) { }
    internal Form1(NewsStore? store)
    {
        _store=store??new NewsStore();
        InitializeComponent();
        _reader = new FeedReader(_store);
        Text = "RSS Reader Lite"; MinimumSize = new Size(1050, 650); Size = new Size(1420, 900); StartPosition = FormStartPosition.CenterScreen;
        BuildUi(); Load += OnLoaded; Shown += OnShown; FormClosing += OnClosing;
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, RowCount=2, Padding=new Padding(8) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,30)); Controls.Add(root);
        _navigation.Dock=DockStyle.Fill; _navigation.IntegralHeight=false; _navigation.Font=new Font("Segoe UI",10); _navigation.Items.AddRange(["ホーム","おすすめ","新着","未読","お気に入りメディア","記事のお気に入り","カテゴリ","RSSフィード"]); _navigation.SelectedIndexChanged+=(_,_)=>{ if(_navigation.SelectedItem is string s){_view=s; RefreshArticles();} }; root.Controls.Add(_navigation,0,0);
        var main=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2}; main.RowStyles.Add(new RowStyle(SizeType.Absolute,48)); main.RowStyles.Add(new RowStyle(SizeType.Percent,100)); root.Controls.Add(main,1,0);
        var toolbar=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,AutoScroll=true,Padding=new Padding(2)};
        _refresh.Text="今すぐ更新"; _refresh.AutoSize=true; _refresh.Click+=async(_,_)=>await RefreshAllAsync(); toolbar.Controls.Add(_refresh);
        AddButton(toolbar,"初回セットアップ",(_,_)=>OpenSetupWizard()); AddButton(toolbar,"フィード管理",(_,_)=>ManageFeeds()); AddButton(toolbar,"カテゴリ",(_,_)=>ManageCategories()); AddButton(toolbar,"分類ルール",(_,_)=>ManageRules()); AddButton(toolbar,"興味設定",(_,_)=>ManageInterests());
        toolbar.Controls.Add(new Label{Text="自動更新(分)",AutoSize=true,Padding=new Padding(4,9,0,0)}); _interval.DropDownStyle=ComboBoxStyle.DropDownList;_interval.Width=75;_interval.Items.AddRange(["0","5","10","15","30","60"]);var saved=_store.GetSetting("refresh_minutes","10");_interval.SelectedItem=_interval.Items.Contains(saved)?saved:"10";_interval.SelectedIndexChanged+=(_,_)=>SetRefreshInterval();toolbar.Controls.Add(_interval);
        _search.Width=210;_search.TextChanged+=(_,_)=>RefreshArticles();toolbar.Controls.Add(new Label{Text="検索",AutoSize=true,Padding=new Padding(2,9,0,0)});toolbar.Controls.Add(_search);main.Controls.Add(toolbar,0,0);
        var split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Vertical};Shown+=(_,_)=>{if(split.Width>700){split.Panel1MinSize=360;split.Panel2MinSize=300;split.SplitterDistance=Math.Min(620,split.Width-320);}};main.Controls.Add(split,0,1);
        _articles.Dock=DockStyle.Fill;_articles.ReadOnly=true;_articles.AllowUserToAddRows=false;_articles.AllowUserToDeleteRows=false;_articles.SelectionMode=DataGridViewSelectionMode.FullRowSelect;_articles.MultiSelect=false;_articles.AutoGenerateColumns=false;_articles.RowHeadersVisible=false;_articles.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.None;
        _articles.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Title",HeaderText="タイトル",AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill,MinimumWidth=180});
        _articles.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Media",HeaderText="メディア",Width=130});
        _articles.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Published",HeaderText="日時",Width=145,DefaultCellStyle=new DataGridViewCellStyle{Format="yyyy/MM/dd HH:mm"}});
        _articles.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Categories",HeaderText="カテゴリ",Width=160});
        _articles.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Url",HeaderText="URL",Width=120});_articles.SelectionChanged+=(_,_)=>ShowSelected();_articles.CellDoubleClick+=(_,_)=>OpenExternal();_articles.KeyDown+=(_,e)=>{if(e.Control&&e.KeyCode==Keys.Enter)OpenExternal();};split.Panel1.Controls.Add(_articles);
        var browserPanel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};browserPanel.RowStyles.Add(new RowStyle(SizeType.Absolute,40));browserPanel.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var browserToolbar=new FlowLayoutPanel{Dock=DockStyle.Fill}; AddButton(browserToolbar,"戻る",(_,_)=>{if(_browser.CanGoBack)_browser.GoBack();}); AddButton(browserToolbar,"進む",(_,_)=>{if(_browser.CanGoForward)_browser.GoForward();});AddButton(browserToolbar,"再読込",(_,_)=>_browser.Reload());AddButton(browserToolbar,"外部で開く",(_,_)=>OpenExternal());browserPanel.Controls.Add(browserToolbar,0,0);_browser.Dock=DockStyle.Fill;browserPanel.Controls.Add(_browser,0,1);split.Panel2.Controls.Add(browserPanel);
        _status.Dock=DockStyle.Fill;_status.TextAlign=ContentAlignment.MiddleLeft;root.Controls.Add(_status,0,1);root.SetColumnSpan(_status,2);
        _refreshTimer.Tick+=async(_,_)=>await RefreshAllAsync();
    }
    private static void AddButton(Control parent,string text,EventHandler action){var b=new Button{Text=text,AutoSize=true,Height=32};b.Click+=action;parent.Controls.Add(b);}
    private void OnLoaded(object? sender,EventArgs e)
    {
        try
        {
            _needsSetup=!_store.IsSetupComplete;
            StartupDiagnostics.WriteInfo($"Main window loaded. SetupComplete={_store.IsSetupComplete}; NeedsSetup={_needsSetup}; Database={_store.DatabasePath}");
            _navigation.SelectedIndex=0;
            SetRefreshInterval();
            if(_needsSetup)_status.Text="初回セットアップを開始します…";
        }
        catch(Exception ex){ShowStartupFailure(ex);Close();}
    }
    private void OnShown(object? sender,EventArgs e)
    {
        if(_shownInitializationStarted)return;
        _shownInitializationStarted=true;
        // Schedule initialization after the initial Shown message has returned. This ensures
        // the owner has a native visible window before ShowDialog is called on first launch.
        BeginInvoke(new Action(async()=>await InitializeAfterShownAsync()));
    }
    private async Task InitializeAfterShownAsync()
    {
        try
        {
            if(_needsSetup)
            {
                if(!OpenSetupWizard())return;
            }
            try{await _browser.EnsureCoreWebView2Async();}
            catch(Exception ex){StartupDiagnostics.Write(ex,"WebView2 initialization");_status.Text=$"内蔵ブラウザを利用できません。外部ブラウザは利用できます。詳細: {StartupDiagnostics.LogPath}";}
            if(_store.GetFeeds().Count>0)_=RefreshAllAsync();
        }
        catch(Exception ex){ShowStartupFailure(ex);}
    }
    private bool OpenSetupWizard()
    {
        try
        {
            using var wizard=new SetupWizardDialog();
            StartupDiagnostics.WriteInfo($"Opening setup wizard. OwnerVisible={Visible}; IsHandleCreated={IsHandleCreated}");
            if(wizard.ShowDialog(this)!=DialogResult.OK)
            {
                StartupDiagnostics.WriteInfo("Setup wizard was cancelled.");
                if(_needsSetup){Close();return false;}
                return true;
            }
            _store.CompleteInitialSetup(wizard.SelectedFeeds,wizard.SelectedInterests,wizard.CustomInterests,wizard.RefreshMinutes);
            _needsSetup=false;
            _interval.SelectedItem=wizard.RefreshMinutes.ToString();
            _status.Text="初回セットアップが完了しました。";
            StartupDiagnostics.WriteInfo("Setup wizard completed.");
            return true;
        }
        catch(Exception ex){ShowStartupFailure(ex);return false;}
    }
    private void ShowStartupFailure(Exception ex){StartupDiagnostics.Write(ex,"Main window startup");MessageBox.Show(this,$"アプリの起動処理でエラーが発生しました。\r\n\r\n{ex.Message}\r\n\r\n診断ログ: {StartupDiagnostics.LogPath}","RSS Reader Lite 起動エラー",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    private void OnClosing(object? sender,FormClosingEventArgs e){_refreshTimer.Stop();_lifetime.Cancel();_lifetime.Dispose();_browser.Dispose();}
    private void SetRefreshInterval(){if(_interval.SelectedItem is not string value)return;var minutes=int.Parse(value);_store.SetSetting("refresh_minutes",minutes.ToString());_refreshTimer.Stop();if(minutes>0)_refreshTimer.Interval=minutes*60_000; if(minutes>0)_refreshTimer.Start();}
    private async Task RefreshAllAsync(){if(_loading||IsDisposed)return;_loading=true;_refresh.Enabled=false;try{var feeds=_store.GetFeeds().Where(x=>x.Enabled).ToList();int failed=0;foreach(var feed in feeds){if(_lifetime.IsCancellationRequested)break;await _reader.RefreshAsync(feed,_lifetime.Token);if(feed.LastResult.StartsWith("失敗"))failed++;}RefreshArticles();_status.Text=$"更新完了：{feeds.Count-failed}/{feeds.Count}フィード成功　{DateTime.Now:HH:mm:ss}";}catch(OperationCanceledException){}catch(Exception ex){StartupDiagnostics.Write(ex,"RSS refresh");if(!IsDisposed)_status.Text=$"更新エラー。詳細: {StartupDiagnostics.LogPath}";}finally{_loading=false;if(!IsDisposed)_refresh.Enabled=true;}}
    private void RefreshArticles(){if(IsDisposed)return;try{if(_view=="カテゴリ"&&_selectedCategoryId==null){var cats=_store.GetCategories();if(cats.Count>0){using var d=new SelectDialog("カテゴリ",cats.Cast<object>().ToList());if(d.ShowDialog(this)==DialogResult.OK&&d.Selected is Category c)_selectedCategoryId=c.Id;}}
        var rows=_store.GetArticles(_view,_search.Text,_view=="カテゴリ"?_selectedCategoryId:null);_articles.DataSource=rows;foreach(DataGridViewRow r in _articles.Rows)if(r.DataBoundItem is Article a&&a.IsRead)r.DefaultCellStyle.ForeColor=SystemColors.GrayText;_status.Text=$"{rows.Count} 件";
    }catch(Exception ex){_status.Text=$"表示エラー: {ex.Message}";}}
    private void ShowSelected(){if(_articles.CurrentRow?.DataBoundItem is not Article a)return;_store.SetArticleFlag(a.Id,"IsRead",true);a.IsRead=true;try{if(Uri.TryCreate(a.Url,UriKind.Absolute,out var uri)&&uri.Scheme is "http" or "https")_browser.Source=uri;}catch{} }
    private void OpenExternal(){if(_articles.CurrentRow?.DataBoundItem is Article a&&Uri.TryCreate(a.Url,UriKind.Absolute,out var uri)&&uri.Scheme is "http" or "https")try{Process.Start(new ProcessStartInfo(uri.ToString()){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(this,ex.Message,"ブラウザを開けません",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
    private void ManageFeeds(){using var d=new FeedManagerDialog(_store);if(d.ShowDialog(this)==DialogResult.OK){RefreshArticles();}}
    private void ManageCategories(){using var d=new CategoryManagerDialog(_store);d.ShowDialog(this);}
    private void ManageRules(){var cats=_store.GetCategories();if(cats.Count==0){MessageBox.Show(this,"先にカテゴリを作成してください。");return;}using var d=new RuleDialog(_store,cats);if(d.ShowDialog(this)==DialogResult.OK)RefreshArticles();}
    private void ManageInterests(){using var d=new InterestDialog(_store,_store.GetCategories());d.ShowDialog(this);if(_view=="おすすめ")RefreshArticles();}
}

internal sealed class SelectDialog : Form
{
    private readonly ListBox _list=new();
    public object? Selected=>_list.SelectedItem;
    public SelectDialog(string title,List<object> options)
    {Text=title;Width=400;Height=400;StartPosition=FormStartPosition.CenterParent;_list.Dock=DockStyle.Fill;_list.Items.AddRange(options.ToArray());Controls.Add(_list);_list.DoubleClick+=(_,_)=>{DialogResult=DialogResult.OK;Close();};}
}
