using System.Runtime.InteropServices;

namespace BanchoNET.Core.Utils;

public static partial class FileLinking
{
    // ReSharper disable once InconsistentNaming
    [LibraryImport("libc", EntryPoint = "link", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int link(string oldPath, string newPath);

    public static bool TryCreateHardLink(string sourcePath, string destPath) =>
        OperatingSystem.IsLinux() && link(sourcePath, destPath) == 0;

    public static void LinkOrCopy(string sourcePath, string destPath)
    {
        if (File.Exists(destPath))
            File.Delete(destPath);

        if (!TryCreateHardLink(sourcePath, destPath))
            File.Copy(sourcePath, destPath);
    }
}