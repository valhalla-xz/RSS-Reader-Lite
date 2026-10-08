using System.ServiceModel.Syndication;
using System.Net.Http;
using System.Xml;

namespace WinFormsApp1;

public sealed class FeedReader
{
    private readonly NewsStore store;
    private readonly HttpClient client;
    public FeedReader(NewsStore store,bool bypassSystemProxy=false,HttpMessageHandler? handler=null)
    {
        this.store=store;client=new HttpClient(handler??new HttpClientHandler{UseProxy=!bypassSystemProxy,AutomaticDecompression=System.Net.DecompressionMethods.GZip|System.Net.DecompressionMethods.Deflate}){Timeout=TimeSpan.FromSeconds(20)};
    }

    public async Task<int> RefreshAsync(Feed feed, CancellationToken cancellationToken)
    {
        try
        {
            if (!Uri.TryCreate(feed.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) throw new InvalidOperationException("有効なHTTP/HTTPS URLではありません。");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri); request.Headers.UserAgent.ParseAdd("LocalNewsReader/1.0 (+RSS reader)");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode(); using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 12_000_000 });
            var syndication = SyndicationFeed.Load(reader) ?? throw new InvalidDataException("フィードを解析できませんでした。");
            int inserted = 0; var now = DateTimeOffset.Now;
            foreach (var item in syndication.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var link = item.Links.FirstOrDefault(x => x.RelationshipType == "alternate")?.Uri?.ToString() ?? item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";
                var content = item.Content is TextSyndicationContent text ? text.Text : "";
                var summary = item.Summary?.Text ?? ""; var guid = item.Id;
                if (string.IsNullOrWhiteSpace(guid)) guid = !string.IsNullOrWhiteSpace(link) ? link : $"{item.Title?.Text}|{item.PublishDate:O}";
                var article = new Article { FeedId=feed.Id, Title=item.Title?.Text?.Trim() ?? "(タイトルなし)", Url=link, Guid=guid, Published=item.PublishDate==DateTimeOffset.MinValue?null:item.PublishDate, Fetched=now, Summary=summary, Content=content };
                store.SaveArticle(article); inserted++;
            }
            feed.LastFetched=now; feed.LastResult=$"成功（{inserted}件確認）"; store.UpdateFeedResult(feed); return inserted;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { feed.LastFetched=DateTimeOffset.Now; feed.LastResult="失敗：タイムアウト"; store.UpdateFeedResult(feed); return 0; }
        catch (Exception ex)
        { feed.LastFetched=DateTimeOffset.Now; feed.LastResult=$"失敗：{ex.Message}"; store.UpdateFeedResult(feed); return 0; }
    }
}
