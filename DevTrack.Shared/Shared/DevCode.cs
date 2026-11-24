using System.Diagnostics.CodeAnalysis;

namespace Shared;

public static class DevCode
{
    #region String validation

    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
    {
        #region Check whether text is missing

        return string.IsNullOrWhiteSpace(value);

        #endregion
    }

    #endregion
}
