using RichTexteEditor.Core.Model;
using Xunit;

namespace RichTexteEditor.Core.Tests.Model;

/// <summary>
/// Tests for Tag inline element - hashtag references.
/// </summary>
public class TagTests
{
    [Fact]
    public void WhenCreatedWithNameThenNameIsSet()
    {
        var tag = new Tag("project");

        Assert.Equal("project", tag.Name);
    }

    [Fact]
    public void WhenCreatedWithNullNameThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Tag(null!));
    }

    [Fact]
    public void WhenCreatedWithEmptyNameThenThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Tag(string.Empty));
    }

    [Fact]
    public void WhenCreatedWithWhitespaceNameThenThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Tag("   "));
    }

    [Fact]
    public void WhenCreatedWithHashPrefixThenHashIsStripped()
    {
        var tag = new Tag("#project");

        Assert.Equal("project", tag.Name);
    }

    [Fact]
    public void WhenNameContainsHyphenThenAllowed()
    {
        var tag = new Tag("high-priority");

        Assert.Equal("high-priority", tag.Name);
    }

    [Fact]
    public void WhenNameContainsUnderscoreThenAllowed()
    {
        var tag = new Tag("meeting_notes");

        Assert.Equal("meeting_notes", tag.Name);
    }

    [Fact]
    public void WhenNameContainsNumbersThenAllowed()
    {
        var tag = new Tag("project123");

        Assert.Equal("project123", tag.Name);
    }

    [Fact]
    public void WhenNameStartsWithNumberThenThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Tag("123project"));
    }

    [Fact]
    public void WhenNameContainsSpacesThenThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Tag("my project"));
    }

    [Fact]
    public void WhenTwoTagsHaveSameNameThenAreEqual()
    {
        var tag1 = new Tag("project");
        var tag2 = new Tag("project");

        Assert.Equal(tag1, tag2);
    }

    [Fact]
    public void WhenTwoTagsHaveDifferentNamesThenAreNotEqual()
    {
        var tag1 = new Tag("project");
        var tag2 = new Tag("task");

        Assert.NotEqual(tag1, tag2);
    }

    [Fact]
    public void WhenTagsComparedCaseInsensitiveThenAreEqual()
    {
        var tag1 = new Tag("Project");
        var tag2 = new Tag("project");

        // Tags should be case-insensitive for matching
        Assert.True(tag1.Matches(tag2));
    }

    [Fact]
    public void WhenInlineTypeRequestedThenReturnsTag()
    {
        var tag = new Tag("project");

        Assert.Equal(InlineType.Tag, tag.Type);
    }

    [Fact]
    public void WhenPlainTextRequestedThenReturnsHashtagFormat()
    {
        var tag = new Tag("project");

        Assert.Equal("#project", tag.ToPlainText());
    }

    [Fact]
    public void WhenToStringCalledThenReturnsHashtagFormat()
    {
        var tag = new Tag("project");

        Assert.Equal("#project", tag.ToString());
    }
}
