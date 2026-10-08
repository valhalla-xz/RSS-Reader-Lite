namespace WinFormsApp1;

public sealed class Feed
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool Favorite { get; set; }
    public DateTimeOffset? LastFetched { get; set; }
    public string LastResult { get; set; } = "未取得";
    public override string ToString() => Name;
}

public sealed class Article
{
    public long Id { get; set; }
    public long FeedId { get; set; }
    public string Title { get; set; } = "(タイトルなし)";
    public string Url { get; set; } = "";
    public string Guid { get; set; } = "";
    public DateTimeOffset? Published { get; set; }
    public DateTimeOffset Fetched { get; set; }
    public string Summary { get; set; } = "";
    public string Content { get; set; } = "";
    public string Media { get; set; } = "";
    public bool IsRead { get; set; }
    public bool IsFavorite { get; set; }
    public string Categories { get; set; } = "";
}

public sealed class Category
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public override string ToString() => FullPath;
}

public sealed class ClassificationRule
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public string Pattern { get; set; } = "";
    public string Field { get; set; } = "タイトル＋本文";
    public string Operator { get; set; } = "OR";
    public bool IsRegex { get; set; }
    public int Priority { get; set; }
}

public sealed class Interest
{
    public long Id { get; set; }
    public string Term { get; set; } = "";
    public long? CategoryId { get; set; }
}

public sealed class SetupInterest
{
    public string Name { get; set; } = "";
    public List<string> Terms { get; set; } = new();
    public List<string> CategoryPaths { get; set; } = new();
    public override string ToString() => Name;
}
