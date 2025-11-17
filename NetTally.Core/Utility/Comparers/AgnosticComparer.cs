using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using NetTally.Configure;
using NetTally.Models;

namespace NetTally.Utility.Comparers;

/// <summary>
/// Class that provides access to variable string comparers depending on
/// quest preferences.
/// </summary>
public class Agnostic
{
    public Agnostic(QuestsInfo info, IHash hash)
    {
        questsInfo = info;
        InitDependencies(hash);
        self = this;
    }

    static Agnostic self = AppX.Services.GetRequiredService<Agnostic>();

    /// <summary>
    /// Enum for the different combinations of comparison patterns.
    /// </summary>
    private enum AgnosticPattern
    {
        Unknown,
        NoCaseSymbol,
        NoCaseNoSymbol,
        CaseSymbol,
        CaseNoSymbol
    }

    public AgnosticStringComparer StringComparerNoCaseSymbol { get; private set; } = null!;

    public AgnosticStringComparer StringComparerNoCaseNoSymbol { get; private set; } = null!;

    public AgnosticStringComparer StringComparerCaseSymbol { get; private set; } = null!;

    public AgnosticStringComparer StringComparerCaseNoSymbol { get; private set; } = null!;

    static QuestsInfo questsInfo = null!;

    /// <summary>
    /// Get the current string comparer for the currently selected quest.
    /// </summary>
    public static AgnosticStringComparer CurrentStringComparer
    {
        get
        {
            var agnosticPattern = GetCurrentQuestComparisonPattern(questsInfo.SelectedQuest);

            return agnosticPattern switch
            {
                AgnosticPattern.Unknown => self.StringComparerNoCaseNoSymbol,
                AgnosticPattern.NoCaseSymbol => self.StringComparerNoCaseSymbol,
                AgnosticPattern.NoCaseNoSymbol => self.StringComparerNoCaseNoSymbol,
                AgnosticPattern.CaseSymbol => self.StringComparerCaseSymbol,
                AgnosticPattern.CaseNoSymbol => self.StringComparerCaseNoSymbol,
                _ => throw new NotImplementedException()
            };
        }
    }

    /// <summary>
    /// Get a case-insensitive string comparer.
    /// </summary>
    public static AgnosticStringComparer CaseInsensitiveComparer => self.StringComparerNoCaseSymbol;

    /// <summary>
    /// Get a string comparer that ignores both case and symbols.
    /// </summary>
    public static AgnosticStringComparer InsensitiveComparer => self.StringComparerNoCaseNoSymbol;

    /// <summary>
    /// Get the agnostic pattern for the given quest, based on
    /// settings for case and whitespace significance.
    /// </summary>
    /// <param name="quest"></param>
    /// <returns>An <see cref="AgnosticPattern"/> that matches the quest provided.</returns>
    private static AgnosticPattern GetCurrentQuestComparisonPattern(Quest? quest)
    {
        if (quest == null)
        {
            return AgnosticPattern.Unknown;
        }

        return quest.CaseIsSignificant switch
        {
            true => quest.WhitespaceAndPunctuationIsSignificant switch
            {
                true => AgnosticPattern.CaseSymbol,
                false => AgnosticPattern.CaseNoSymbol
            },
            false => quest.WhitespaceAndPunctuationIsSignificant switch
            {
                true => AgnosticPattern.NoCaseSymbol,
                false => AgnosticPattern.NoCaseNoSymbol
            }
        };
    }

    /// <summary>
    /// Initialize string comparers and dependency injection information.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    private void InitDependencies(IHash hashFunction)
    {
        StringComparerNoCaseSymbol = new AgnosticStringComparer(CultureInfo.InvariantCulture.CompareInfo,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth,
            hashFunction.HashFunction);

        // Case insensitive, whitespace/symbol insensitive
        StringComparerNoCaseNoSymbol = new AgnosticStringComparer(CultureInfo.InvariantCulture.CompareInfo,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth,
            hashFunction.HashFunction);

        // Case sensitive, whitespace/symbol sensitive.
        StringComparerCaseSymbol = new AgnosticStringComparer(CultureInfo.InvariantCulture.CompareInfo,
            CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth,
            hashFunction.HashFunction);

        // Case sensitive, whitespace/symbol insensitive.
        StringComparerCaseNoSymbol = new AgnosticStringComparer(CultureInfo.InvariantCulture.CompareInfo,
            CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth,
            hashFunction.HashFunction);
    }
}
