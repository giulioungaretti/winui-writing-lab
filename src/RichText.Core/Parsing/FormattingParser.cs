using RichTexteEditor.Core.Model;

namespace RichTexteEditor.Core.Parsing;

/// <summary>
/// Parses markdown-style formatting markers in plain text into formatted text runs.
/// Supported spans: <c>**bold**</c>, <c>*italic*</c>, <c>***bold italic***</c>,
/// <c>~~strikethrough~~</c>, and <c>`code`</c>. Markers must directly wrap the content
/// (no leading/trailing whitespace inside the span); unmatched or empty markers are
/// treated as literal text. This is the inverse of <see cref="Rendering.MarkdownRenderer"/>.
/// </summary>
public static class FormattingParser
{
    // Ordered longest-first so that at the same position "***" wins over "**" over "*".
    private static readonly (string Marker, TextFormatting Format)[] Markers =
    [
        ("***", TextFormatting.Bold | TextFormatting.Italic),
        ("**", TextFormatting.Bold),
        ("~~", TextFormatting.Strikethrough),
        ("*", TextFormatting.Italic),
        ("`", TextFormatting.Code),
    ];

    /// <summary>
    /// Parses the text into one or more text runs with formatting applied.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>Text runs covering the input; plain input yields a single unformatted run.</returns>
    public static IReadOnlyList<TextRun> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return [];
        }

        return ParseCore(text, TextFormatting.None);
    }

    private static List<TextRun> ParseCore(string text, TextFormatting inherited)
    {
        var result = new List<TextRun>();
        var index = 0;

        while (index < text.Length)
        {
            var (start, marker, format) = FindNextSpan(text, index);

            if (start < 0)
            {
                result.Add(new TextRun(text[index..], inherited));
                break;
            }

            if (start > index)
            {
                result.Add(new TextRun(text[index..start], inherited));
            }

            var contentStart = start + marker.Length;
            var end = FindClose(text, contentStart, marker);
            var inner = text[contentStart..end];

            if (format == TextFormatting.Code || (inherited | format) == inherited)
            {
                // Code spans are literal; no nested parsing.
                result.Add(new TextRun(inner, inherited | format));
            }
            else
            {
                result.AddRange(ParseCore(inner, inherited | format));
            }

            index = end + marker.Length;
        }

        return result;
    }

    private static (int Index, string Marker, TextFormatting Format) FindNextSpan(string text, int startIndex)
    {
        var bestIndex = -1;
        var bestMarker = string.Empty;
        var bestFormat = TextFormatting.None;

        foreach (var (marker, format) in Markers)
        {
            var searchFrom = startIndex;

            while (searchFrom < text.Length)
            {
                var open = text.IndexOf(marker, searchFrom, StringComparison.Ordinal);
                if (open < 0 || (bestIndex >= 0 && open >= bestIndex))
                {
                    break;
                }

                if (FindClose(text, open + marker.Length, marker) >= 0)
                {
                    bestIndex = open;
                    bestMarker = marker;
                    bestFormat = format;
                    break;
                }

                searchFrom = open + marker.Length;
            }
        }

        return (bestIndex, bestMarker, bestFormat);
    }

    /// <summary>
    /// Finds the closing marker for a span opened at <paramref name="contentStart"/> - marker.Length.
    /// Skips candidate closes that would leave whitespace-trimmed or marker-unbalanced content,
    /// so nested spans such as <c>**bold *and italic***</c> resolve to the outermost close.
    /// </summary>
    private static int FindClose(string text, int contentStart, string marker)
    {
        var searchFrom = contentStart;

        while (searchFrom < text.Length)
        {
            var close = text.IndexOf(marker, searchFrom, StringComparison.Ordinal);
            if (close < 0)
            {
                return -1;
            }

            var inner = text[contentStart..close];
            if (IsValidSpanContent(inner, marker[0]) && HasBalancedMarkers(inner, marker[0]))
            {
                return close;
            }

            searchFrom = close + 1;
        }

        return -1;
    }

    private static bool HasBalancedMarkers(string inner, char markerChar)
    {
        var count = 0;
        foreach (var c in inner)
        {
            if (c == markerChar)
            {
                count++;
            }
        }

        return count % 2 == 0;
    }

    private static bool IsValidSpanContent(string inner, char markerChar)
    {
        return inner.Length > 0
            && !char.IsWhiteSpace(inner[0])
            && !char.IsWhiteSpace(inner[^1])
            && inner.Trim(markerChar).Length > 0;
    }
}
