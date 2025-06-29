using System.IO;

namespace BugPrince.Util;

internal static class FileUtil
{
    internal static void AtomicWrite(string path, string content)
    {
        string temp = Path.Combine(Path.GetTempPath(), Path.GetFileName(path));
        try
        {
            File.WriteAllText(temp, content);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}
