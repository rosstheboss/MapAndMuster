using MapAndMuster.Domain.News;

namespace MapAndMuster.Backend.UnitTests.News;

public sealed class NewsMarkdownTests
{
    [Fact]
    public void EncodesHtmlThenAppliesMarkdown()
    {
        var html = NewsMarkdown.ToHtml("# Hello <script>\n\nA **bold** and *italic* [link](https://example.test).");
        Assert.Contains("<h1>Hello &lt;script&gt;</h1>", html, StringComparison.Ordinal);
        Assert.Contains("<strong>bold</strong>", html, StringComparison.Ordinal);
        Assert.Contains("<em>italic</em>", html, StringComparison.Ordinal);
        Assert.Contains("href=\"https://example.test/\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsNonHttpLinks()
    {
        var html = NewsMarkdown.ToHtml("[x](javascript:alert(1))");
        Assert.DoesNotContain("javascript", html, StringComparison.Ordinal);
        Assert.Contains("x", html, StringComparison.Ordinal);
    }
}

public sealed class NewsArticleRulesTests
{
    [Fact]
    public void AcceptsAValidArticle()
    {
        Assert.True(NewsArticleRules.TryCreate("Frontier update", "The roads are open.", out var title, out var body, out _));
        Assert.Equal("Frontier update", title);
        Assert.Equal("The roads are open.", body);
    }

    [Fact]
    public void RejectsEmptyOrAbusiveCopy()
    {
        Assert.False(NewsArticleRules.TryCreate("ab", "Body", out _, out _, out var shortTitle));
        Assert.Equal("news.title.length", shortTitle!.Code);
        Assert.False(NewsArticleRules.TryCreate("Hello", "   ", out _, out _, out var empty));
        Assert.Equal("news.body.required", empty!.Code);
    }
}
