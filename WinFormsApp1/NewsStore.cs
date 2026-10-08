using Microsoft.Data.Sqlite;

namespace WinFormsApp1;

public sealed class NewsStore
{
    private readonly string _connectionString;

    public NewsStore(string? databasePath=null)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalNewsReader");
        Directory.CreateDirectory(directory);
        var path=databasePath??Path.Combine(directory,"news.db");Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA foreign_keys=ON;
            CREATE TABLE IF NOT EXISTS Feeds(Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Url TEXT NOT NULL UNIQUE, Enabled INTEGER NOT NULL DEFAULT 1, Favorite INTEGER NOT NULL DEFAULT 0, LastFetched TEXT, LastResult TEXT NOT NULL DEFAULT '未取得');
            CREATE TABLE IF NOT EXISTS Articles(Id INTEGER PRIMARY KEY, FeedId INTEGER NOT NULL REFERENCES Feeds(Id) ON DELETE CASCADE, Title TEXT NOT NULL, Url TEXT NOT NULL, Guid TEXT NOT NULL, Published TEXT, Fetched TEXT NOT NULL, Summary TEXT NOT NULL DEFAULT '', Content TEXT NOT NULL DEFAULT '', IsRead INTEGER NOT NULL DEFAULT 0, IsFavorite INTEGER NOT NULL DEFAULT 0, UNIQUE(FeedId, Guid));
            CREATE INDEX IF NOT EXISTS IX_Articles_Published ON Articles(Published DESC);
            CREATE TABLE IF NOT EXISTS Categories(Id INTEGER PRIMARY KEY, ParentId INTEGER REFERENCES Categories(Id) ON DELETE SET NULL, Name TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Rules(Id INTEGER PRIMARY KEY, CategoryId INTEGER NOT NULL REFERENCES Categories(Id) ON DELETE CASCADE, Pattern TEXT NOT NULL, Field TEXT NOT NULL, Operator TEXT NOT NULL, IsRegex INTEGER NOT NULL, Priority INTEGER NOT NULL DEFAULT 0);
            CREATE TABLE IF NOT EXISTS Interests(Id INTEGER PRIMARY KEY, Term TEXT NOT NULL, CategoryId INTEGER REFERENCES Categories(Id) ON DELETE CASCADE);
            CREATE TABLE IF NOT EXISTS Settings(Key TEXT PRIMARY KEY, Value TEXT NOT NULL);
            INSERT OR IGNORE INTO Settings(Key,Value) VALUES('refresh_minutes','10');
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open() { var db = new SqliteConnection(_connectionString); db.Open(); using(var c=db.CreateCommand()){c.CommandText="PRAGMA foreign_keys=ON;";c.ExecuteNonQuery();}return db; }
    private static string? DateText(DateTimeOffset? value) => value?.ToString("O");
    private static DateTimeOffset? ParseDate(string? value) => DateTimeOffset.TryParse(value, out var date) ? date : null;

    public List<Feed> GetFeeds()
    {
        using var db = Open(); using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT Id,Name,Url,Enabled,Favorite,LastFetched,LastResult FROM Feeds ORDER BY Name";
        using var r = cmd.ExecuteReader(); var list = new List<Feed>();
        while (r.Read()) list.Add(new Feed { Id=r.GetInt64(0), Name=r.GetString(1), Url=r.GetString(2), Enabled=r.GetInt64(3)!=0, Favorite=r.GetInt64(4)!=0, LastFetched=ParseDate(r.IsDBNull(5)?null:r.GetString(5)), LastResult=r.GetString(6) });
        return list;
    }

    public void SaveFeed(Feed f)
    {
        if(string.IsNullOrWhiteSpace(f.Name))throw new ArgumentException("フィード名を入力してください。");
        if(!Uri.TryCreate(f.Url,UriKind.Absolute,out var uri)||uri.Scheme!="http"&&uri.Scheme!="https")throw new ArgumentException("http:// または https:// で始まる有効なURLを入力してください。");
        f.Name=f.Name.Trim();f.Url=f.Url.Trim();
        using var db=Open(); using var cmd=db.CreateCommand();
        cmd.CommandText="INSERT INTO Feeds(Id,Name,Url,Enabled,Favorite,LastFetched,LastResult) VALUES($id,$n,$u,$e,$f,$d,$r) ON CONFLICT(Id) DO UPDATE SET Name=$n,Url=$u,Enabled=$e,Favorite=$f,LastFetched=$d,LastResult=$r";
        cmd.Parameters.AddWithValue("$id",f.Id==0?DBNull.Value:f.Id); cmd.Parameters.AddWithValue("$n",f.Name); cmd.Parameters.AddWithValue("$u",f.Url); cmd.Parameters.AddWithValue("$e",f.Enabled?1:0); cmd.Parameters.AddWithValue("$f",f.Favorite?1:0); cmd.Parameters.AddWithValue("$d",(object?)DateText(f.LastFetched)??DBNull.Value); cmd.Parameters.AddWithValue("$r",f.LastResult); cmd.ExecuteNonQuery();
        if(f.Id==0) { using var id=db.CreateCommand(); id.CommandText="SELECT last_insert_rowid()"; f.Id=(long)(id.ExecuteScalar()??0L); }
    }
    public void DeleteFeed(long id) { using var db=Open(); using var c=db.CreateCommand(); c.CommandText="DELETE FROM Feeds WHERE Id=$id"; c.Parameters.AddWithValue("$id",id); c.ExecuteNonQuery(); }
    public void SaveArticle(Article a)
    {
        using var db=Open(); using var c=db.CreateCommand(); c.CommandText="INSERT OR IGNORE INTO Articles(FeedId,Title,Url,Guid,Published,Fetched,Summary,Content) VALUES($feed,$title,$url,$guid,$published,$fetched,$summary,$content)";
        c.Parameters.AddWithValue("$feed",a.FeedId); c.Parameters.AddWithValue("$title",a.Title); c.Parameters.AddWithValue("$url",a.Url); c.Parameters.AddWithValue("$guid",a.Guid); c.Parameters.AddWithValue("$published",(object?)DateText(a.Published)??DBNull.Value); c.Parameters.AddWithValue("$fetched",DateText(a.Fetched)!); c.Parameters.AddWithValue("$summary",a.Summary); c.Parameters.AddWithValue("$content",a.Content); c.ExecuteNonQuery();
    }
    public void UpdateFeedResult(Feed f) { using var db=Open(); using var c=db.CreateCommand(); c.CommandText="UPDATE Feeds SET LastFetched=$d,LastResult=$r WHERE Id=$id"; c.Parameters.AddWithValue("$d",(object?)DateText(f.LastFetched)??DBNull.Value); c.Parameters.AddWithValue("$r",f.LastResult); c.Parameters.AddWithValue("$id",f.Id); c.ExecuteNonQuery(); }
    public List<Article> GetArticles(string view, string? search=null, long? categoryId=null)
    {
        using var db=Open(); using var c=db.CreateCommand();
        var where=view switch { "未読"=>"AND a.IsRead=0", "お気に入りメディア"=>"AND f.Favorite=1", "記事のお気に入り"=>"AND a.IsFavorite=1", _=>"" };
        if(!string.IsNullOrWhiteSpace(search)) where += " AND (a.Title LIKE $q OR a.Summary LIKE $q OR f.Name LIKE $q)";
        c.CommandText=$"SELECT a.Id,a.FeedId,a.Title,a.Url,a.Guid,a.Published,a.Fetched,a.Summary,a.Content,a.IsRead,a.IsFavorite,f.Name FROM Articles a JOIN Feeds f ON f.Id=a.FeedId WHERE 1=1 {where} ORDER BY COALESCE(a.Published,a.Fetched) DESC LIMIT 2000";
        if(categoryId.HasValue)c.Parameters.AddWithValue("$cat",categoryId.Value); if(!string.IsNullOrWhiteSpace(search))c.Parameters.AddWithValue("$q",$"%{search}%");
        using var r=c.ExecuteReader(); var list=new List<Article>(); while(r.Read()) list.Add(new Article{Id=r.GetInt64(0),FeedId=r.GetInt64(1),Title=r.GetString(2),Url=r.GetString(3),Guid=r.GetString(4),Published=ParseDate(r.IsDBNull(5)?null:r.GetString(5)),Fetched=ParseDate(r.GetString(6))??DateTimeOffset.Now,Summary=r.GetString(7),Content=r.GetString(8),IsRead=r.GetInt64(9)!=0,IsFavorite=r.GetInt64(10)!=0,Media=r.GetString(11)});
        var rules=GetRules(); var cats=GetCategories().ToDictionary(x=>x.Id,x=>x.FullPath); var interests=GetInterests();
        var ranked=list.Select(article=>{
            var matches=rules.Where(rule=>RuleMatches(rule,article)).Select(rule=>rule.CategoryId).Distinct().ToList();
            article.Categories=string.Join(", ",matches.Where(cats.ContainsKey).Select(id=>cats[id]));
            int score=(article.IsFavorite?4:0)+matches.Count*3+rules.Where(rule=>RuleMatches(rule,article)).Sum(rule=>Math.Max(0,rule.Priority));
            foreach(var interest in interests){if(interest.CategoryId.HasValue&&matches.Contains(interest.CategoryId.Value))score+=5; if(!string.IsNullOrWhiteSpace(interest.Term)&&Contains(article.Title+" "+article.Summary+" "+article.Content,interest.Term))score+=2;}
            return (article,score);
        });
        if(categoryId.HasValue)ranked=ranked.Where(x=>rules.Any(rule=>rule.CategoryId==categoryId.Value&&RuleMatches(rule,x.article)));
        if(view=="おすすめ")ranked=ranked.Where(x=>x.score>0).OrderByDescending(x=>x.score).ThenByDescending(x=>x.article.Published??x.article.Fetched);
        return ranked.Select(x=>x.article).ToList();
    }
    private static bool RuleMatches(ClassificationRule rule,Article article)
    {
        var source=rule.Field switch{"タイトル"=>article.Title,"本文"=>article.Content+" "+article.Summary,_=>article.Title+" "+article.Content+" "+article.Summary};
        var terms=rule.Pattern.Split(new[]{';',',','\n','\r'},StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray(); if(terms.Length==0)return false;
        var results=terms.Select(term=>{try{return rule.IsRegex?System.Text.RegularExpressions.Regex.IsMatch(source,term,System.Text.RegularExpressions.RegexOptions.IgnoreCase|System.Text.RegularExpressions.RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100)):Contains(source,term);}catch{return false;}});
        var matches=results.ToList();return rule.Operator.StartsWith("NOT",StringComparison.OrdinalIgnoreCase)?!matches.Any(x=>x):rule.Operator=="AND"?matches.All(x=>x):matches.Any(x=>x);
    }
    private static bool Contains(string source,string term)=>source.IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0;
    public void SetArticleFlag(long id,string column,bool value) { if(column is not ("IsRead" or "IsFavorite")) throw new ArgumentOutOfRangeException(nameof(column)); using var db=Open(); using var c=db.CreateCommand(); c.CommandText=$"UPDATE Articles SET {column}=$v WHERE Id=$id"; c.Parameters.AddWithValue("$v",value?1:0); c.Parameters.AddWithValue("$id",id); c.ExecuteNonQuery(); }
    public string GetSetting(string key,string fallback) { using var db=Open(); using var c=db.CreateCommand(); c.CommandText="SELECT Value FROM Settings WHERE Key=$k"; c.Parameters.AddWithValue("$k",key); return c.ExecuteScalar() as string ?? fallback; }
    public void SetSetting(string key,string value) { using var db=Open(); using var c=db.CreateCommand(); c.CommandText="INSERT INTO Settings(Key,Value) VALUES($k,$v) ON CONFLICT(Key) DO UPDATE SET Value=$v"; c.Parameters.AddWithValue("$k",key); c.Parameters.AddWithValue("$v",value); c.ExecuteNonQuery(); }
    public bool IsSetupComplete => string.Equals(GetSetting("setup_completed","false"),"true",StringComparison.OrdinalIgnoreCase);
    public void CompleteInitialSetup(IEnumerable<Feed> feeds,IEnumerable<SetupInterest> interests,IEnumerable<string> customInterests,int refreshMinutes)
    {
        using var db=Open();using var tx=db.BeginTransaction();
        long InsertCategory(string name,long? parent){using var c=db.CreateCommand();c.Transaction=tx;c.CommandText="SELECT Id FROM Categories WHERE ParentId IS $p AND Name=$n LIMIT 1";c.Parameters.AddWithValue("$p",(object?)parent??DBNull.Value);c.Parameters.AddWithValue("$n",name);var found=c.ExecuteScalar();if(found!=null)return Convert.ToInt64(found);c.CommandText="INSERT INTO Categories(ParentId,Name) VALUES($p,$n); SELECT last_insert_rowid();";return (long)c.ExecuteScalar()!;}
        long InsertRule(long category,string terms){using var c=db.CreateCommand();c.Transaction=tx;c.CommandText="SELECT Id FROM Rules WHERE CategoryId=$c AND Pattern=$p LIMIT 1";c.Parameters.AddWithValue("$c",category);c.Parameters.AddWithValue("$p",terms);var found=c.ExecuteScalar();if(found!=null)return Convert.ToInt64(found);c.CommandText="INSERT INTO Rules(CategoryId,Pattern,Field,Operator,IsRegex,Priority) VALUES($c,$p,'タイトル＋本文','OR',0,0); SELECT last_insert_rowid();";return (long)c.ExecuteScalar()!;}
        var ids=new Dictionary<string,long>();
        long Root(string name)=>ids[name]=InsertCategory(name,null);
        long Child(string path,string name){var parentPath=path.Substring(0,path.LastIndexOf('>')).Trim();var parent=ids[parentPath];return ids[path]=InsertCategory(name,parent);}
        Root("国内ニュース");Root("世界ニュース");Root("テクノロジー");Child("テクノロジー > AI","AI");Root("PC・OS");Child("PC・OS > Windows","Windows");Child("PC・OS > Linux","Linux");Child("PC・OS > ハードウェア","ハードウェア");Root("自動車");Child("自動車 > EV","EV");Root("モータースポーツ");Root("ゲーム");Root("科学・環境");
        InsertRule(ids["テクノロジー > AI"],"AI;artificial intelligence;machine learning;generative AI;生成AI;人工知能");
        InsertRule(ids["PC・OS > Windows"],"Windows;Microsoft");InsertRule(ids["PC・OS > Linux"],"Linux");InsertRule(ids["PC・OS > ハードウェア"],"PC;processor;GPU;Radeon;GeForce;ハードウェア");
        InsertRule(ids["自動車 > EV"],"EV;BEV;electric vehicle;electric car;battery vehicle;電気自動車");InsertRule(ids["モータースポーツ"],"Formula 1;F1;motorsport;モータースポーツ");InsertRule(ids["ゲーム"],"game;gaming;video game;ゲーム");InsertRule(ids["科学・環境"],"science;NASA;climate;科学;宇宙;環境");
        foreach(var feed in feeds){using var c=db.CreateCommand();c.Transaction=tx;c.CommandText="INSERT OR IGNORE INTO Feeds(Name,Url,Enabled,Favorite,LastResult) VALUES($n,$u,1,$f,'未取得')";c.Parameters.AddWithValue("$n",feed.Name.Trim());c.Parameters.AddWithValue("$u",feed.Url.Trim());c.Parameters.AddWithValue("$f",feed.Favorite?1:0);c.ExecuteNonQuery();}
        var uniqueTerms=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var interest in interests){var categoryIds=interest.CategoryPaths.Where(ids.ContainsKey).Select(x=>ids[x]).ToList();var first=true;foreach(var term in interest.Terms.Select(x=>x.Trim()).Where(x=>x.Length>0)){if(!uniqueTerms.Add(term))continue;using var c=db.CreateCommand();c.Transaction=tx;c.CommandText="INSERT INTO Interests(Term,CategoryId) SELECT $t,$category WHERE NOT EXISTS(SELECT 1 FROM Interests WHERE Term=$t AND CategoryId IS $category)";c.Parameters.AddWithValue("$t",term);c.Parameters.AddWithValue("$category",first&&categoryIds.Count>0?(object)categoryIds[0]:DBNull.Value);c.ExecuteNonQuery();first=false;}if(first&&categoryIds.Count>0){using var c=db.CreateCommand();c.Transaction=tx;c.CommandText="INSERT INTO Interests(Term,CategoryId) SELECT $t,$category WHERE NOT EXISTS(SELECT 1 FROM Interests WHERE Term=$t AND CategoryId=$category)";c.Parameters.AddWithValue("$t",interest.Name);c.Parameters.AddWithValue("$category",categoryIds[0]);c.ExecuteNonQuery();}}
        foreach(var term in customInterests.Select(x=>x.Trim()).Where(x=>x.Length>0))if(uniqueTerms.Add(term)){using var c=db.CreateCommand();c.Transaction=tx;c.CommandText="INSERT INTO Interests(Term) SELECT $t WHERE NOT EXISTS(SELECT 1 FROM Interests WHERE Term=$t)";c.Parameters.AddWithValue("$t",term);c.ExecuteNonQuery();}
        using(var c=db.CreateCommand()){c.Transaction=tx;c.CommandText="INSERT INTO Settings(Key,Value) VALUES('refresh_minutes',$m),('setup_completed','true') ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value";c.Parameters.AddWithValue("$m",refreshMinutes.ToString());c.ExecuteNonQuery();}tx.Commit();
    }
    public List<Category> GetCategories() { using var db=Open(); using var c=db.CreateCommand(); c.CommandText="SELECT Id,ParentId,Name FROM Categories ORDER BY Name"; using var r=c.ExecuteReader(); var rows=new List<Category>(); while(r.Read()) rows.Add(new Category{Id=r.GetInt64(0),ParentId=r.IsDBNull(1)?null:r.GetInt64(1),Name=r.GetString(2)}); var byId=rows.ToDictionary(x=>x.Id); string PathFor(Category x,HashSet<long> seen){if(!seen.Add(x.Id))return x.Name;return x.ParentId.HasValue&&byId.TryGetValue(x.ParentId.Value,out var p)?PathFor(p,seen)+" > "+x.Name:x.Name;} foreach(var x in rows)x.FullPath=PathFor(x,[]);return rows; }
    public void SaveCategory(Category c) { ValidateCategoryName(c.Name);using var db=Open(); using var cmd=db.CreateCommand(); cmd.CommandText="INSERT INTO Categories(ParentId,Name) VALUES($p,$n)"; cmd.Parameters.AddWithValue("$p",(object?)c.ParentId??DBNull.Value); cmd.Parameters.AddWithValue("$n",c.Name.Trim()); cmd.ExecuteNonQuery(); }
    public void UpdateCategory(Category c) { ValidateCategoryName(c.Name);var all=GetCategories();var self=all.FirstOrDefault(x=>x.Id==c.Id);if(self==null)throw new InvalidOperationException("カテゴリが見つかりません。");if(c.ParentId==c.Id||all.Any(x=>x.Id==c.ParentId&&x.FullPath.StartsWith(self.FullPath+" > ",StringComparison.Ordinal)))throw new InvalidOperationException("自分自身または子カテゴリを親にできません。");using var db=Open();using var cmd=db.CreateCommand();cmd.CommandText="UPDATE Categories SET ParentId=$p,Name=$n WHERE Id=$id";cmd.Parameters.AddWithValue("$p",(object?)c.ParentId??DBNull.Value);cmd.Parameters.AddWithValue("$n",c.Name.Trim());cmd.Parameters.AddWithValue("$id",c.Id);cmd.ExecuteNonQuery(); }
    private static void ValidateCategoryName(string name){if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("カテゴリ名を入力してください。");if(name.Trim().Length>80)throw new ArgumentException("カテゴリ名は80文字以内で入力してください。");}
    public void DeleteCategory(long id) { using var db=Open(); using var c=db.CreateCommand(); c.CommandText="DELETE FROM Categories WHERE Id=$id"; c.Parameters.AddWithValue("$id",id); c.ExecuteNonQuery(); }
    public List<ClassificationRule> GetRules() { using var db=Open(); using var c=db.CreateCommand(); c.CommandText="SELECT Id,CategoryId,Pattern,Field,Operator,IsRegex,Priority FROM Rules ORDER BY Priority DESC"; using var r=c.ExecuteReader(); var l=new List<ClassificationRule>(); while(r.Read())l.Add(new(){Id=r.GetInt64(0),CategoryId=r.GetInt64(1),Pattern=r.GetString(2),Field=r.GetString(3),Operator=r.GetString(4),IsRegex=r.GetInt64(5)!=0,Priority=(int)r.GetInt64(6)}); return l; }
    public void SaveRule(ClassificationRule x) { if(string.IsNullOrWhiteSpace(x.Pattern))throw new ArgumentException("分類ルールの条件を入力してください。");if(x.CategoryId<=0)throw new ArgumentException("カテゴリを選択してください。");using var db=Open(); using var c=db.CreateCommand(); c.CommandText=x.Id==0?"INSERT INTO Rules(CategoryId,Pattern,Field,Operator,IsRegex,Priority) VALUES($c,$p,$f,$o,$r,$n)":"UPDATE Rules SET CategoryId=$c,Pattern=$p,Field=$f,Operator=$o,IsRegex=$r,Priority=$n WHERE Id=$id"; c.Parameters.AddWithValue("$c",x.CategoryId); c.Parameters.AddWithValue("$p",x.Pattern.Trim()); c.Parameters.AddWithValue("$f",x.Field); c.Parameters.AddWithValue("$o",x.Operator); c.Parameters.AddWithValue("$r",x.IsRegex?1:0); c.Parameters.AddWithValue("$n",x.Priority);if(x.Id!=0)c.Parameters.AddWithValue("$id",x.Id);c.ExecuteNonQuery(); }
    public void DeleteRule(long id) { using var db=Open();using var c=db.CreateCommand();c.CommandText="DELETE FROM Rules WHERE Id=$id";c.Parameters.AddWithValue("$id",id);c.ExecuteNonQuery(); }
    public List<Interest> GetInterests() { using var db=Open(); using var c=db.CreateCommand(); c.CommandText="SELECT Id,Term,CategoryId FROM Interests"; using var r=c.ExecuteReader(); var l=new List<Interest>(); while(r.Read())l.Add(new(){Id=r.GetInt64(0),Term=r.GetString(1),CategoryId=r.IsDBNull(2)?null:r.GetInt64(2)}); return l; }
    public void SaveInterest(string term,long? categoryId) { if(string.IsNullOrWhiteSpace(term))throw new ArgumentException("興味キーワードを入力してください。");using var db=Open(); using var c=db.CreateCommand(); c.CommandText="INSERT INTO Interests(Term,CategoryId) VALUES($t,$c)"; c.Parameters.AddWithValue("$t",term.Trim()); c.Parameters.AddWithValue("$c",(object?)categoryId??DBNull.Value); c.ExecuteNonQuery(); }
}
