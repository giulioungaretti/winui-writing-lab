using RichTexteEditor.Core.Model;
using Xunit;

namespace RichTexteEditor.Core.Tests.Model;

/// <summary>
/// Tests for TextFormatting flags enum operations.
/// </summary>
public class TextFormattingTests
{
    [Fact]
    public void WhenNoFormattingThenValueIsZero()
    {
        var formatting = TextFormatting.None;

        Assert.Equal(0, (int)formatting);
    }

    [Fact]
    public void WhenBoldThenHasBoldFlag()
    {
        var formatting = TextFormatting.Bold;

        Assert.True(formatting.HasFlag(TextFormatting.Bold));
        Assert.False(formatting.HasFlag(TextFormatting.Italic));
    }

    [Fact]
    public void WhenCombiningFlagsThenAllFlagsPresent()
    {
        var formatting = TextFormatting.Bold | TextFormatting.Italic;

        Assert.True(formatting.HasFlag(TextFormatting.Bold));
        Assert.True(formatting.HasFlag(TextFormatting.Italic));
        Assert.False(formatting.HasFlag(TextFormatting.Underline));
    }

    [Fact]
    public void WhenRemovingFlagThenFlagNoLongerPresent()
    {
        var formatting = TextFormatting.Bold | TextFormatting.Italic;

        formatting &= ~TextFormatting.Bold;

        Assert.False(formatting.HasFlag(TextFormatting.Bold));
        Assert.True(formatting.HasFlag(TextFormatting.Italic));
    }

    [Fact]
    public void WhenTogglingFlagThenStateInverts()
    {
        var formatting = TextFormatting.Bold;

        formatting ^= TextFormatting.Bold;
        Assert.False(formatting.HasFlag(TextFormatting.Bold));

        formatting ^= TextFormatting.Bold;
        Assert.True(formatting.HasFlag(TextFormatting.Bold));
    }
}
