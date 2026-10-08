using System.Net;
using System.Net.Http;
using System.Text;
using WinFormsApp1;

internal static class Program
{
    private static int _passed;
    private static int Main()
    {
        Application.EnableVisualStyles();
        try
        {
            TestSetupWizardGui();
            Task.Run(()=>RunStoreAndFeedTests()).GetAwaiter().GetResult();
            Console.WriteLine($"PASS: {_passed} checks");
            return 0;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine("FAIL: "+ex);
            return 1;
        }
    }

    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);_passed++;Console.WriteLine("PASS: "+message);}
    private static void Throws<T>(Action action,string message) where T:Exception{try{action();}catch(T){Check(true,message);return;}throw new InvalidOperationException("Expected "+typeof(T).Name+": "+message);}

    private static void TestSetupWizardGui()
    {
        using var wizard=new SetupWizardDialog();wizard.Show();Application.DoEvents();
        var lists=FindAll<CheckedListBox>(wizard);var feedList=lists.Single(x=>x.Items.Count>20&&x.Items[0] is FeedOption);var interestList=lists.Single(x=>x.Items.Count>20&&x.Items[0] is SetupInterest);var originalFeedCount=feedList.Items.Count;
        Check(wizard.Controls.OfType<TabControl>().Single().TabPages.Count==3,"初回セットアップは取得先・関心・確認の3画面を表示");
        Check(feedList.CheckedItems.Count>=6&&feedList.Items.Count>=30&&feedList.Items.Cast<object>().Any(x=>x.ToString()!.Contains("nippon.com"))&&feedList.Items.Cast<object>().Any(x=>x.ToString()!.Contains("Le Monde Sports")),"国内/海外と複数ジャンルを含む30件以上の初期フィード候補を表示");
        Check(interestList.Items.Count>=20&&interestList.Items.Cast<object>().Any(x=>x.ToString()!.Contains("医療・健康"))&&interestList.Items.Cast<object>().Any(x=>x.ToString()!.Contains("旅行・観光")),"関心カテゴリに政治・経済から健康・文化・旅行までを用意");
        var sourceGroup=FindAll<GroupBox>(wizard).Single(x=>x.Text.Contains("RSS/Atom URL"));var sourceInputs=FindAll<TextBox>(sourceGroup);sourceInputs[0].Text="テスト追加フィード";sourceInputs[1].Text="https://example.test/feed.xml";FindAll<Button>(sourceGroup).Single(x=>x.Text=="追加").PerformClick();Application.DoEvents();
        Check(feedList.Items.Count==originalFeedCount+1&&feedList.CheckedItems.Cast<object>().Any(x=>x.ToString()!.Contains("テスト追加フィード")),"ウィザード画面から任意RSS URLを追加し選択");
        interestList.SetItemChecked(Enumerable.Range(0,interestList.Items.Count).Single(i=>interestList.Items[i].ToString()!.Contains("PC・半導体・GPU")),true);
        FindAll<ComboBox>(wizard).Single().SelectedItem="15";
        var customGroup=FindAll<GroupBox>(wizard).Single(x=>x.Text.Contains("追加のキーワード"));customGroup.Controls.OfType<TextBox>().Single().Text="SpaceX";
        wizard.Controls.OfType<TabControl>().Single().SelectedIndex=2;Application.DoEvents();
        FindAll<Button>(wizard).Single(x=>x.Text=="ニュースを読み始める").PerformClick();Application.DoEvents();
        Check(wizard.DialogResult==DialogResult.OK,"ウィザードの完了操作で設定結果を確定");
        Check(wizard.SelectedFeeds.Count>=4&&wizard.SelectedInterests.Any(x=>x.Name=="PC・半導体・GPU")&&wizard.CustomInterests.Contains("SpaceX")&&wizard.RefreshMinutes==15,"フィード・関心・自由語・更新間隔をウィザードから取得");

        var launchFolder=Path.Combine(Path.GetTempPath(),"LocalNewsReader-first-run-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(launchFolder);
        try
        {
            using var mainWindow=new Form1(new NewsStore(Path.Combine(launchFolder,"first-run.db")));var wizardOwnerVisible=false;var wizardOpened=false;
            Check(FindAll<Button>(mainWindow).Any(x=>x.Text=="初回セットアップ"),"メイン画面から初回セットアップを再表示できる");
            using var dismissTimer=new System.Windows.Forms.Timer{Interval=100};dismissTimer.Tick+=(_,_)=>{var dialog=Application.OpenForms.OfType<SetupWizardDialog>().FirstOrDefault();if(dialog==null)return;wizardOpened=true;wizardOwnerVisible=dialog.Owner?.Visible==true;dialog.DialogResult=DialogResult.Cancel;dialog.Close();};dismissTimer.Start();
            mainWindow.Show();var deadline=DateTime.UtcNow.AddSeconds(2);while(!wizardOpened&&DateTime.UtcNow<deadline){Application.DoEvents();Thread.Sleep(10);}dismissTimer.Stop();Application.DoEvents();
            Check(wizardOpened&&wizardOwnerVisible,"初回ウィザードを表示済みメイン画面の子として開く");
        }
        finally{try{Directory.Delete(launchFolder,true);}catch{}}
    }

    private static async Task RunStoreAndFeedTests()
    {
        var folder=Path.Combine(Path.GetTempPath(),"LocalNewsReader-smoke-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        const string rss="""<?xml version="1.0" encoding="utf-8"?><rss version="2.0"><channel><title>テストニュース</title><link>https://example.test</link><description>Fixture</description><item><title>EV battery breakthrough</title><link>https://example.test/ev-1</link><guid>ev-1</guid><pubDate>Wed, 07 Oct 2026 12:00:00 GMT</pubDate><description>EV battery technology</description></item></channel></rss>""";
        var handler=new FixtureHandler(new[]{(rss,HttpStatusCode.OK),(rss,HttpStatusCode.OK),("<html>not a feed</html>",HttpStatusCode.OK),("",HttpStatusCode.ServiceUnavailable)});
        try
        {
            var path=Path.Combine(folder,"test.db");var store=new NewsStore(path);var feed=new Feed{Name="ローカルfixture",Url="https://fixture.invalid/feed"};
            store.CompleteInitialSetup(new[]{feed},new[]{new SetupInterest{Name="自動車・EV",Terms={"EV","electric vehicle"},CategoryPaths={"自動車 > EV"}}},new[]{"Radeon"},15);
            Check(store.IsSetupComplete,"初回設定と初期カテゴリ/ルール/フィードをSQLiteに保存");
            Check(store.GetCategories().Any(x=>x.FullPath=="自動車 > EV")&&store.GetRules().Count>=8,"階層カテゴリと既定分類ルールを作成");
            Check(store.GetInterests().Any(x=>x.Term=="Radeon"),"興味キーワードを永続化");
            Throws<ArgumentException>(()=>store.SaveCategory(new Category{Name="   "}),"空白だけの分類名を拒否");
            using(var categoryEdit=new CategoryEditDialog(store.GetCategories())){var input=FindAll<TextBox>(categoryEdit).Single();input.Text="　 ";Check(!categoryEdit.TryValidateName(out _),"分類名ダイアログでも全角/半角空白だけの入力を拒否");input.Text="PC";Check(categoryEdit.TryValidateName(out _),"有効な分類名を受理");}
            Throws<ArgumentException>(()=>store.SaveRule(new ClassificationRule{CategoryId=1,Pattern="  "}),"空白だけの分類条件を拒否");
            var loaded=store.GetFeeds().Single();loaded.Name="名前変更後";store.SaveFeed(loaded);
            Check(store.GetFeeds().Count==1&&store.GetFeeds()[0].Name=="名前変更後","既存フィード編集で新規行を重複作成しない");
            var reader=new FeedReader(store,handler:handler);await reader.RefreshAsync(loaded,new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);await reader.RefreshAsync(loaded,new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
            Check(store.GetArticles("新着").Count==1,"RSS記事を取得し、再取得時にGUIDで重複を防止");
            var ev=store.GetCategories().Single(x=>x.FullPath=="自動車 > EV");
            Check(store.GetArticles("カテゴリ",categoryId:ev.Id).Count==1&&store.GetArticles("おすすめ").Count==1,"既定ルールでカテゴリ分類とおすすめに反映");
            var cars=store.GetCategories().Single(x=>x.FullPath=="自動車");Check(store.GetArticles("カテゴリ",categoryId:cars.Id).Count==1,"親カテゴリの表示に子カテゴリの記事を含める");
            var cats=store.GetCategories();var game=cats.Single(x=>x.FullPath=="ゲーム");var world=cats.Single(x=>x.FullPath=="世界ニュース");var ai=cats.Single(x=>x.FullPath=="テクノロジー > AI");
            store.SaveRule(new ClassificationRule{CategoryId=game.Id,Pattern="EV;battery",Operator="AND",Field="タイトル"});store.SaveRule(new ClassificationRule{CategoryId=world.Id,Pattern="blocked;excluded",Operator="NOT（いずれも含まない）",Field="タイトル"});store.SaveRule(new ClassificationRule{CategoryId=ai.Id,Pattern="^EV",Field="タイトル",IsRegex=true,Priority=9});
            Check(store.GetArticles("カテゴリ",categoryId:game.Id).Count==1&&store.GetArticles("カテゴリ",categoryId:world.Id).Count==1&&store.GetArticles("カテゴリ",categoryId:ai.Id).Count==1,"AND/NOT/正規表現ルールを記事のカテゴリ判定に適用");
            var bad=new Feed{Name="壊れたfixture",Url="https://fixture.invalid/broken"};store.SaveFeed(bad);var broken=store.GetFeeds().Single(x=>x.Name=="壊れたfixture");await reader.RefreshAsync(broken,new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
            Check(broken.LastResult.StartsWith("失敗")&&store.GetArticles("新着").Count==1,"不正XMLをフィード単位の失敗として記録し、既存記事を保持");
            var unavailable=new Feed{Name="HTTPエラーfixture",Url="https://fixture.invalid/unavailable"};store.SaveFeed(unavailable);var httpError=store.GetFeeds().Single(x=>x.Name=="HTTPエラーfixture");await reader.RefreshAsync(httpError,new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);Check(httpError.LastResult.StartsWith("失敗"),"HTTP 503を取得失敗として記録");
            var reloaded=new NewsStore(path);Check(reloaded.IsSetupComplete&&reloaded.GetArticles("新着").Count==1,"別Storeインスタンスで再起動後のデータ保持を確認");
        }
        finally{try{Directory.Delete(folder,true);}catch{}}
    }

    private sealed class FixtureHandler : HttpMessageHandler
    {
        private readonly Queue<(string Body,HttpStatusCode Status)> _responses;
        public FixtureHandler(IEnumerable<(string Body,HttpStatusCode Status)> responses)=>_responses=new Queue<(string,HttpStatusCode)>(responses);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            if(_responses.Count==0)throw new InvalidOperationException("Unexpected RSS request: "+request.RequestUri);
            var response=_responses.Dequeue();var message=new HttpResponseMessage(response.Status){Content=new StringContent(response.Body,Encoding.UTF8,"application/rss+xml")};return Task.FromResult(message);
        }
    }

    private static List<T> FindAll<T>(Control root) where T:Control
    {var results=new List<T>();foreach(Control child in root.Controls){if(child is T match)results.Add(match);results.AddRange(FindAll<T>(child));}return results;}
}
