namespace BugPrince.Util;

internal static class StringUtil
{
    internal static bool ConsumePrefix(this string self, string prefix, out string suffix)
    {
        suffix = "";
        if (!self.StartsWith(prefix)) return false;

        suffix = self.Substring(prefix.Length);
        return true;
    }
}
