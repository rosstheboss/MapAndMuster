using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MapAndMuster.Domain.News;

/// <summary>
/// Converts administrator markdown to HTML after HTML-encoding the source so raw markup cannot run.
/// </summary>
public static partial class NewsMarkdown
{
    /// <summary>
    /// Renders a conservative markdown subset to HTML.
    /// </summary>
    /// <param name="markdown">The article markdown.</param>
    /// <returns>Encoded HTML suitable for a news board.</returns>
    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var encoded = WebUtility.HtmlEncode(markdown.Replace("\r\n", "\n", StringComparison.Ordinal));
        var blocks = encoded.Split("\n\n", StringSplitOptions.None);
        var html = new StringBuilder();
        foreach (var block in blocks)
        {
            var trimmed = block.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var lines = trimmed.Split('\n');
            if (lines.All(static line => UnorderedItem().IsMatch(line.Trim())))
            {
                html.Append("<ul>");
                foreach (var line in lines)
                {
                    var item = UnorderedItem().Match(line.Trim()).Groups[1].Value;
                    html.Append("<li>").Append(Inline(item)).Append("</li>");
                }

                html.Append("</ul>");
                continue;
            }

            if (Heading().Match(trimmed) is { Success: true } heading)
            {
                var level = Math.Clamp(heading.Groups[1].Value.Length, 1, 3);
                html.Append("<h").Append(level).Append('>')
                    .Append(Inline(heading.Groups[2].Value.Trim()))
                    .Append("</h").Append(level).Append('>');
                continue;
            }

            html.Append("<p>").Append(Inline(trimmed.Replace('\n', ' '))).Append("</p>");
        }

        return html.ToString();
    }

    private static string Inline(string text)
    {
        var withCode = InlineCode().Replace(text, "<code>$1</code>");
        var withBold = Bold().Replace(withCode, "<strong>$1</strong>");
        var withItalic = Italic().Replace(withBold, "<em>$1</em>");
        return Link().Replace(withItalic, match =>
        {
            var href = WebUtility.HtmlDecode(match.Groups[2].Value);
            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri)
                || uri.Scheme is not ("http" or "https"))
            {
                return match.Groups[1].Value;
            }

            return $"<a href=\"{WebUtility.HtmlEncode(uri.ToString())}\" rel=\"noopener noreferrer\">{match.Groups[1].Value}</a>";
        });
    }

    [GeneratedRegex(@"^(#{1,3})\s+(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex Heading();

    [GeneratedRegex(@"^[-*]\s+(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex UnorderedItem();

    [GeneratedRegex(@"`([^`]+)`", RegexOptions.CultureInvariant)]
    private static partial Regex InlineCode();

    [GeneratedRegex(@"\*\*([^*]+)\*\*", RegexOptions.CultureInvariant)]
    private static partial Regex Bold();

    [GeneratedRegex(@"(?<!\*)\*([^*]+)\*(?!\*)", RegexOptions.CultureInvariant)]
    private static partial Regex Italic();

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex Link();
}
