using System.Runtime.CompilerServices;

namespace NetTally.Utility.Strings;

public static class FormatTemplates
{
    public static string FormatWith(this string template, params object?[] args)
    {
        return FormattableStringFactory.Create(template, args).ToString();
    }

    // todo: update when fixed
    // New extension style is buggy when using params as of RC2.
    // Fixed in https://github.com/dotnet/roslyn/pull/80433
    //extension(string template)
    //{
    //    public string FormatWith(params object?[] prm)
    //    {
    //        return FormattableStringFactory.Create(template, prm).ToString();
    //    }
    //}
}
