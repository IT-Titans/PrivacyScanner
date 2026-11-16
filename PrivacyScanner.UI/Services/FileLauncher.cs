using System.Diagnostics;
using System.IO;

namespace ITTitans.PrivacyScanner.UI.Services;

/// <summary><see cref="IFileLauncher"/> backed by the file system and the Windows shell.</summary>
public class FileLauncher : IFileLauncher
{
    public bool Exists(string path) => File.Exists(path);

    public void Open(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
