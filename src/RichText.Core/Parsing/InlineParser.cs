using System.Text.RegularExpressions;
using RichTexteEditor.Core.Model;

namespace RichTexteEditor.Core.Parsing;

/// <summary>
/// Parses text input into inline elements, handling tags, todos, and plain text.
/// </summary>
public partial class InlineParser
{
    // Pattern for hashtags: # followed by letter, then letters/numbers/hyphens/underscores
    private static readonly Regex TagPattern = TagRegex();

    // Pattern for bracket todos: [ ] or [x] or [X] followed by text
    private static readonly Regex BracketTodoPattern = BracketTodoRegex();

    // Pattern for inline todos: @todo(text) or @done(text)
    private static readonly Regex InlineTodoPattern = InlineTodoRegex();

    // Pattern for escaped hash
    private static readonly Regex EscapedHashPattern = EscapedHashRegex();

    // Pattern for inline images: ![alt](source)
    private static readonly Regex ImagePattern = ImageRegex();

    /// <summary>
    /// Parses the input text into a list of inline elements.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>A list of parsed inline elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    public IReadOnlyList<IInline> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<IInline>();
        }

        var result = new List<IInline>();
        var currentIndex = 0;

        // First, process escaped hashes
        text = ProcessEscapedHashes(text);

        while (currentIndex < text.Length)
        {
            var (nextMatch, matchType) = FindNextMatch(text, currentIndex);

            if (nextMatch == null)
            {
                // No more special elements, add remaining text
                if (currentIndex < text.Length)
                {
                    var remainingText = RestoreEscapedHashes(text[currentIndex..]);
                    result.AddRange(FormattingParser.Parse(remainingText));
                }
                break;
            }

            // Add text before the match
            if (nextMatch.Index > currentIndex)
            {
                var precedingText = RestoreEscapedHashes(text[currentIndex..nextMatch.Index]);
                result.AddRange(FormattingParser.Parse(precedingText));
            }

            // Process the match
            var inline = ProcessMatch(nextMatch, matchType);
            if (inline != null)
            {
                result.Add(inline);
            }

            currentIndex = nextMatch.Index + nextMatch.Length;
        }

        return result;
    }

    private static string ProcessEscapedHashes(string text)
    {
        return EscapedHashPattern.Replace(text, "\x00HASH\x00");
    }

    private static string RestoreEscapedHashes(string text)
    {
        return text.Replace("\x00HASH\x00", "#");
    }

    private (Match? match, MatchType type) FindNextMatch(string text, int startIndex)
    {
        Match? bestMatch = null;
        var bestType = MatchType.None;

        // Check for inline image first: its source/alt may contain characters
        // that would otherwise match the tag pattern (e.g. fragments, anchors).
        var imageMatch = ImagePattern.Match(text, startIndex);
        if (imageMatch.Success)
        {
            bestMatch = imageMatch;
            bestType = MatchType.Image;
        }

        // Check for bracket todo anywhere after startIndex
        var bracketTodoMatch = BracketTodoPattern.Match(text, startIndex);
        if (bracketTodoMatch.Success && (bestMatch == null || bracketTodoMatch.Index < bestMatch.Index))
        {
            // Bracket todos only valid at start of text or after whitespace
            var todoIndex = bracketTodoMatch.Index;
            if (todoIndex == 0 || char.IsWhiteSpace(text[todoIndex - 1]))
            {
                // Ensure there's actual todo text (not just whitespace after checkbox)
                var todoText = bracketTodoMatch.Groups[2].Value;
                if (!string.IsNullOrWhiteSpace(todoText))
                {
                    bestMatch = bracketTodoMatch;
                    bestType = MatchType.BracketTodo;
                }
            }
        }

        // Check for tag
        var tagMatch = TagPattern.Match(text, startIndex);
        if (tagMatch.Success && (bestMatch == null || tagMatch.Index < bestMatch.Index))
        {
            bestMatch = tagMatch;
            bestType = MatchType.Tag;
        }

        // Check for inline todo
        var inlineTodoMatch = InlineTodoPattern.Match(text, startIndex);
        if (inlineTodoMatch.Success && (bestMatch == null || inlineTodoMatch.Index < bestMatch.Index))
        {
            bestMatch = inlineTodoMatch;
            bestType = MatchType.InlineTodo;
        }

        return (bestMatch, bestType);
    }

    private static IInline? ProcessMatch(Match match, MatchType matchType)
    {
        return matchType switch
        {
            MatchType.Tag => CreateTag(match),
            MatchType.BracketTodo => CreateBracketTodo(match),
            MatchType.InlineTodo => CreateInlineTodo(match),
            MatchType.Image => CreateImage(match),
            _ => null
        };
    }

    private static ImageInline? CreateImage(Match match)
    {
        var altText = match.Groups[1].Value;
        var source = match.Groups[2].Value;

        try
        {
            return new ImageInline(source, altText);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static Tag? CreateTag(Match match)
    {
        var tagName = match.Groups[1].Value;
        try
        {
            return new Tag(tagName);
        }
        catch (ArgumentException)
        {
            // Invalid tag name, treat as plain text
            return null;
        }
    }

    private static Todo? CreateBracketTodo(Match match)
    {
        var checkMark = match.Groups[1].Value;
        var todoText = match.Groups[2].Value;
        var isCompleted = checkMark.Equals("x", StringComparison.OrdinalIgnoreCase);

        try
        {
            return new Todo(todoText, isCompleted);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static Todo? CreateInlineTodo(Match match)
    {
        var type = match.Groups[1].Value;
        var todoText = match.Groups[2].Value;
        var isCompleted = type.Equals("done", StringComparison.OrdinalIgnoreCase);

        try
        {
            return new Todo(todoText, isCompleted);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private enum MatchType
    {
        None,
        Tag,
        BracketTodo,
        InlineTodo,
        Image
    }

    [GeneratedRegex(@"#([a-zA-Z][a-zA-Z0-9_-]*)", RegexOptions.Compiled)]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\[([ xX])\]\s+(.+?)(?=\s*(?:#[a-zA-Z]|\[[ xX]\]|@(?:todo|done)\(|$))", RegexOptions.Compiled)]
    private static partial Regex BracketTodoRegex();

    [GeneratedRegex(@"@(todo|done)\(([^)]+)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex InlineTodoRegex();

    [GeneratedRegex(@"\\#", RegexOptions.Compiled)]
    private static partial Regex EscapedHashRegex();

    [GeneratedRegex(@"!\[([^\]]*)\]\(([^)\s]+)\)", RegexOptions.Compiled)]
    private static partial Regex ImageRegex();
}
