using System.Text;

namespace NetTally.Output;

/// <summary>
/// Class that can be put in a using() block to place a spoiler string in
/// the provided string builder at construction and disposal.
/// </summary>
public struct Spoiler : IDisposable
{
    StringBuilder? SB;
    readonly bool Display;

    /// <summary>
    /// Initialize required values so we know whether to display the spoiler tags,
    /// and what label to apply.
    /// </summary>
    /// <param name="sb">The string builder that the text will be added to.</param>
    /// <param name="label">The label for the spoiler.  No spoiler will be displayed if it's null.</param>
    /// <param name="display">Whether we should display the text.</param>
    internal Spoiler(StringBuilder sb, string label, bool display)
    {
        SB = sb;
        Display = display && !string.IsNullOrEmpty(label);

        if (Display)
        {
            SB.AppendLine($"[spoiler=\"{label}\"]");
        }
    }

    /// <summary>
    /// Called when the using() is completed.  Close the tag if we're displaying text.
    /// </summary>
    public void Dispose()
    {
        if (Display)
        {
            SB?.AppendLine($"[/spoiler]");
        }
        SB = null;
    }
}
