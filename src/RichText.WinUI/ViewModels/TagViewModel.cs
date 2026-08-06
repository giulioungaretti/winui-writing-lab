using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RichTexteEditor.ViewModels;

/// <summary>
/// ViewModel for a tag element, following MVVM Toolkit patterns.
/// </summary>
public partial class TagViewModel : ObservableObject
{
    /// <summary>
    /// The tag name (without the # prefix).
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// The display text including the # prefix.
    /// </summary>
    public string DisplayText => $"#{Name}";

    /// <summary>
    /// The position of this tag in the document.
    /// </summary>
    [ObservableProperty]
    private int _startPosition;

    /// <summary>
    /// The end position of this tag in the document.
    /// </summary>
    [ObservableProperty]
    private int _endPosition;

    /// <summary>
    /// Event raised when the tag is clicked.
    /// </summary>
    public event EventHandler<TagClickedEventArgs>? Clicked;

    /// <summary>
    /// Creates a new TagViewModel with the specified name.
    /// </summary>
    /// <param name="name">The tag name without the # prefix.</param>
    public TagViewModel(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Creates a new TagViewModel with position information.
    /// </summary>
    /// <param name="name">The tag name without the # prefix.</param>
    /// <param name="startPosition">Start position in document.</param>
    /// <param name="endPosition">End position in document.</param>
    public TagViewModel(string name, int startPosition, int endPosition)
    {
        Name = name;
        StartPosition = startPosition;
        EndPosition = endPosition;
    }

    /// <summary>
    /// Command executed when the tag is clicked.
    /// </summary>
    [RelayCommand]
    private void Click()
    {
        Clicked?.Invoke(this, new TagClickedEventArgs(Name));
    }
}

/// <summary>
/// Event arguments for tag click events.
/// </summary>
public sealed class TagClickedEventArgs : EventArgs
{
    /// <summary>
    /// The name of the clicked tag.
    /// </summary>
    public string TagName { get; }

    public TagClickedEventArgs(string tagName)
    {
        TagName = tagName;
    }
}
