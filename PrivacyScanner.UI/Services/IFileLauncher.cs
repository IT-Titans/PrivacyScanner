namespace ITTitans.PrivacyScanner.UI.Services;

/// <summary>Checks for files and opens them with the program Windows has registered for them.</summary>
public interface IFileLauncher
{
    bool Exists(string path);

    /// <summary>Opens <paramref name="path"/> through the shell. Throws if the shell cannot start it.</summary>
    void Open(string path);
}
