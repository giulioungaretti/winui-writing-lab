using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace RichTexteEditor.Services;

/// <summary>
/// Represents a detected inline element in the text with its position.
/// </summary>
public sealed record InlineMatch(
    int Start, 
    int End, 
    InlineMatchType Type, 
    string Value, 
    bool IsCompleted = false,
    int CheckboxStart = -1,
    int CheckboxEnd = -1);

/// <summary>
/// Types of inline elements that can be highlighted.
/// </summary>
public enum InlineMatchType
{
    Tag,
    Todo
}

/// <summary>
/// Service for highlighting tags and todos inline within a RichEditBox.
/// Creates Notion-like visual rendering with styled inline elements.
/// </summary>
public sealed partial class InlineHighlighter
{
    // Checkbox characters for inline todo rendering (Notion-like circles)
    // Using standard circle characters that render well at any font size
    public const string UncheckedBox = "?";   // White circle (U+25CB)
    public const string CheckedBox = "?";     // Black circle (U+25CF) - filled when complete
    
    // Alternative checked style with checkmark
    public const string CheckedBoxAlt = "?";  // Fisheye/target circle (U+25C9)

    // Legacy emoji/bracket support for parsing
    public const string UncheckedEmoji = "?";
    public const string CheckedEmoji = "?";

    // Notion-like color palette
    // Tag styling - purple pill/badge (matches screenshot accent)
    private static readonly Color TagForeground = Color.FromArgb(255, 120, 119, 198);  // Soft purple text
    private static readonly Color TagBackground = Color.FromArgb(40, 120, 119, 198);   // Very subtle purple bg
    
    // Todo checkbox colors (Notion-like)
    private static readonly Color TodoUncheckedColor = Color.FromArgb(255, 180, 180, 180);  // Light gray circle
    private static readonly Color TodoCheckedColor = Color.FromArgb(255, 120, 119, 198);    // Purple when done
    
    // Todo text colors
    private static readonly Color TodoTextNormalColor = Color.FromArgb(255, 55, 53, 47);     // Normal text (dark)
    private static readonly Color TodoTextCompletedColor = Color.FromArgb(255, 155, 155, 155);  // Faded when done

    // Regex patterns
    private static readonly Regex TagPattern = TagRegex();
    private static readonly Regex TodoPattern = TodoRegex();
    private static readonly Regex BracketTodoPattern = BracketTodoRegex();

    /// <summary>
    /// Finds all inline elements (tags and todos) in the given text.
    /// </summary>
    public IReadOnlyList<InlineMatch> FindInlines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<InlineMatch>();
        }

        var matches = new List<InlineMatch>();

        // Find all tags (#tagname)
        foreach (Match match in TagPattern.Matches(text))
        {
            matches.Add(new InlineMatch(
                match.Index,
                match.Index + match.Length,
                InlineMatchType.Tag,
                match.Groups[1].Value));
        }

        // Find circle-style todos (?/? followed by text)
        foreach (Match match in TodoPattern.Matches(text))
        {
            var checkbox = match.Groups[1].Value;
            var isCompleted = checkbox == CheckedBox || checkbox == "?" || checkbox == CheckedBoxAlt;
            
            matches.Add(new InlineMatch(
                match.Index,
                match.Index + match.Length,
                InlineMatchType.Todo,
                match.Groups[2].Value.Trim(),
                isCompleted,
                match.Index,
                match.Index + 1));  // Checkbox is single character
        }

        // Find bracket-style todos ([ ] or [x] followed by text)
        foreach (Match match in BracketTodoPattern.Matches(text))
        {
            var checkbox = match.Groups[1].Value;
            var isCompleted = checkbox.Equals("x", StringComparison.OrdinalIgnoreCase);
            
            matches.Add(new InlineMatch(
                match.Index,
                match.Index + match.Length,
                InlineMatchType.Todo,
                match.Groups[2].Value.Trim(),
                isCompleted,
                match.Index,
                match.Index + 3));  // Bracket checkbox is 3 characters "[ ]"
        }

        // Sort by position and remove duplicates (prefer first match at each position)
        return matches
            .GroupBy(m => m.Start)
            .Select(g => g.First())
            .OrderBy(m => m.Start)
            .ToList();
    }

    /// <summary>
    /// Applies rich inline formatting to all tags and todos in the RichEditBox.
    /// Creates Notion-like visual appearance with styled inline elements.
    /// </summary>
    public void ApplyHighlighting(RichEditBox editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        editor.Document.GetText(TextGetOptions.None, out var text);
        var cleanText = text.TrimEnd('\r', '\n');
        
        if (string.IsNullOrEmpty(cleanText))
        {
            return;
        }

        var matches = FindInlines(cleanText);

        // Reset all formatting to default first
        var fullRange = editor.Document.GetRange(0, cleanText.Length);
        ResetFormatting(fullRange);

        // Apply specific formatting to each matched inline element
        foreach (var match in matches)
        {
            switch (match.Type)
            {
                case InlineMatchType.Tag:
                    ApplyTagFormatting(editor, match);
                    break;
                case InlineMatchType.Todo:
                    ApplyTodoFormatting(editor, match);
                    break;
            }
        }
    }

    /// <summary>
    /// Finds the inline element at the specified character position.
    /// </summary>
    public InlineMatch? FindInlineAtPosition(string text, int position)
    {
        var matches = FindInlines(text);
        return matches.FirstOrDefault(m => position >= m.Start && position < m.End);
    }

    /// <summary>
    /// Checks if the position is on a todo checkbox character.
    /// </summary>
    public bool IsOnCheckbox(InlineMatch match, int position)
    {
        if (match.Type != InlineMatchType.Todo || match.CheckboxStart < 0)
        {
            return false;
        }
        // Check if click is on or just after the checkbox (to be more forgiving)
        return position >= match.CheckboxStart && position <= match.CheckboxEnd;
    }

    /// <summary>
    /// Toggles a todo checkbox at the specified position.
    /// </summary>
    public bool ToggleTodoAtPosition(RichEditBox editor, int position)
    {
        ArgumentNullException.ThrowIfNull(editor);

        editor.Document.GetText(TextGetOptions.None, out var text);
        var cleanText = text.TrimEnd('\r', '\n');

        var match = FindInlineAtPosition(cleanText, position);
        
        if (match?.Type != InlineMatchType.Todo)
        {
            return false;
        }

        // Get the checkbox range
        var checkboxRange = editor.Document.GetRange(match.CheckboxStart, match.CheckboxEnd);
        checkboxRange.GetText(TextGetOptions.None, out var currentCheckbox);

        string newCheckbox;
        
        // Handle circle-style checkboxes (? ? ?)
        if (currentCheckbox == UncheckedBox || currentCheckbox == "?")
        {
            newCheckbox = CheckedBox;
        }
        else if (currentCheckbox == CheckedBox || currentCheckbox == "?" || currentCheckbox == CheckedBoxAlt)
        {
            newCheckbox = UncheckedBox;
        }
        // Handle bracket-style checkboxes ([ ] ? [x])
        else if (currentCheckbox.Contains('['))
        {
            var bracketRange = editor.Document.GetRange(match.CheckboxStart, match.CheckboxStart + 3);
            bracketRange.GetText(TextGetOptions.None, out var bracket);
            
            if (bracket == "[ ]")
            {
                bracketRange.SetText(TextSetOptions.None, "[x]");
            }
            else
            {
                bracketRange.SetText(TextSetOptions.None, "[ ]");
            }
            return true;
        }
        else
        {
            return false;
        }

        // Replace circle checkbox
        checkboxRange.SetText(TextSetOptions.None, newCheckbox);
        return true;
    }

    /// <summary>
    /// Converts bracket-style todos to circle-style for cleaner Notion-like appearance.
    /// </summary>
    public string ConvertToCircleStyle(string text)
    {
        // Convert [ ] to ? and [x]/[X] to ?
        var result = text.Replace("[ ]", UncheckedBox);
        result = Regex.Replace(result, @"\[[xX]\]", CheckedBox);
        return result;
    }

    /// <summary>
    /// Resets text formatting to default state.
    /// </summary>
    private static void ResetFormatting(ITextRange range)
    {
        var format = range.CharacterFormat;
        format.ForegroundColor = TodoTextNormalColor;  // Use dark text as default
        format.BackgroundColor = Colors.Transparent;
        format.Strikethrough = FormatEffect.Off;
        // Don't reset Bold/Italic - preserve user formatting
        range.CharacterFormat = format;
    }

    /// <summary>
    /// Applies Notion-like pill/badge styling to a tag (#tagname).
    /// </summary>
    private static void ApplyTagFormatting(RichEditBox editor, InlineMatch match)
    {
        var range = editor.Document.GetRange(match.Start, match.End);
        var format = range.CharacterFormat;
        
        // Pill/badge style: colored text on subtle background
        format.ForegroundColor = TagForeground;
        format.BackgroundColor = TagBackground;
        format.Strikethrough = FormatEffect.Off;
        
        range.CharacterFormat = format;
    }

    /// <summary>
    /// Applies Notion-like styling to a todo item (checkbox + text).
    /// </summary>
    private static void ApplyTodoFormatting(RichEditBox editor, InlineMatch match)
    {
        // Style the checkbox character (? or ?)
        if (match.CheckboxStart >= 0 && match.CheckboxEnd > match.CheckboxStart)
        {
            var checkboxRange = editor.Document.GetRange(match.CheckboxStart, match.CheckboxEnd);
            var checkboxFormat = checkboxRange.CharacterFormat;
            
            // Checkbox color: gray when unchecked, purple when checked
            checkboxFormat.ForegroundColor = match.IsCompleted ? TodoCheckedColor : TodoUncheckedColor;
            checkboxFormat.BackgroundColor = Colors.Transparent;
            checkboxFormat.Strikethrough = FormatEffect.Off;
            
            checkboxRange.CharacterFormat = checkboxFormat;
        }

        // Style the todo text (everything after the checkbox)
        var textStart = match.CheckboxEnd >= 0 ? match.CheckboxEnd : match.Start;
        if (textStart < match.End)
        {
            var textRange = editor.Document.GetRange(textStart, match.End);
            var textFormat = textRange.CharacterFormat;
            
            if (match.IsCompleted)
            {
                // Completed: faded text with strikethrough
                textFormat.ForegroundColor = TodoTextCompletedColor;
                textFormat.Strikethrough = FormatEffect.On;
            }
            else
            {
                // Incomplete: normal dark text
                textFormat.ForegroundColor = TodoTextNormalColor;
                textFormat.Strikethrough = FormatEffect.Off;
            }
            
            textFormat.BackgroundColor = Colors.Transparent;
            textRange.CharacterFormat = textFormat;
        }
    }

    // Regex for #tags: # followed by letter, then letters/numbers/underscore/hyphen
    [GeneratedRegex(@"#([a-zA-Z][a-zA-Z0-9_-]*)", RegexOptions.Compiled)]
    private static partial Regex TagRegex();

    // Circle-style todos: ? or ? or ? followed by space and text (to end of line)
    [GeneratedRegex(@"([???])\s+([^\r\n]+)", RegexOptions.Compiled)]
    private static partial Regex TodoRegex();

    // Bracket-style todos: [ ] or [x]/[X] followed by space and text
    [GeneratedRegex(@"\[([ xX])\]\s+([^\r\n#\[]+)", RegexOptions.Compiled)]
    private static partial Regex BracketTodoRegex();
}
