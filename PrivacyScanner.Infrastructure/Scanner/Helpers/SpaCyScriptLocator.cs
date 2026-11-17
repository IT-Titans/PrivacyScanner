using System.Reflection;
using System.Security.Cryptography;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Helpers;

/// <summary>
/// Finds the spaCy script, or extracts the embedded copy so that Python can run it.
/// </summary>
/// <remarks>
/// Scans run concurrently, in this process and in others (several instances, parallel test processes).
/// The embedded script is therefore never written to a path another scan may be reading or writing.
/// It is written to a private file first and moved into a directory named after its content hash. The move is atomic,
/// so a reader sees the complete script or no file. A file that is already there with the right content
/// is reused, and no lock is held while a scan runs.
/// </remarks>
internal static class SpaCyScriptLocator
{
    internal const string ResourceName = "ITTitans.PrivacyScanner.Infrastructure.Resources.spacy_scan.py";

    private const string ScriptFileName = "spacy_scan.py";

    /// <param name="assembly">Assembly that holds the embedded script.</param>
    /// <param name="assemblyDirectory">Directory of the assembly. A script in its <c>Resources</c> folder takes precedence.</param>
    /// <param name="extractionDirectory">Directory that receives the extracted script.</param>
    /// <param name="resourceName">Name of the embedded resource.</param>
    internal static async Task<string> GetScriptPathAsync(
        Assembly assembly,
        string? assemblyDirectory,
        string extractionDirectory,
        string resourceName,
        CancellationToken cancellationToken)
    {
        // If not running in single-file mode, the file might be in the Resources folder
        if (!string.IsNullOrEmpty(assemblyDirectory))
        {
            var localPath = Path.Combine(assemblyDirectory, "Resources", ScriptFileName);
            if (File.Exists(localPath))
                return localPath;
        }

        // Otherwise extract from embedded resources
        byte[] content;
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                var availableResources = string.Join(", ", assembly.GetManifestResourceNames());
                throw new FileNotFoundException($"Die eingebettete Ressource '{resourceName}' wurde nicht gefunden. Verfügbare Ressourcen: {availableResources}");
            }

            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            content = buffer.ToArray();
        }

        var hash = Convert.ToHexString(SHA256.HashData(content))[..16].ToLowerInvariant();

        // A changed script gets a new directory, so an old copy is never mistaken for the current one.
        // The file name stays spacy_scan.py.
        var scriptDirectory = Path.Combine(extractionDirectory, hash);
        Directory.CreateDirectory(scriptDirectory);
        var scriptPath = Path.Combine(scriptDirectory, ScriptFileName);
        if (await CheckAsync(scriptPath, content, cancellationToken) == ScriptState.Complete)
            return scriptPath;

        var partialPath = Path.Combine(scriptDirectory, $"{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(partialPath, content, cancellationToken);

            try
            {
                File.Move(partialPath, scriptPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The name is taken. Another scan published the same script, unless the file is damaged.
                if (await CheckAsync(scriptPath, content, cancellationToken) == ScriptState.Damaged)
                {
                    // Damaged, or edited by hand: replace it.
                    File.Move(partialPath, scriptPath, overwrite: true);
                }
            }
        }
        finally
        {
            File.Delete(partialPath);
        }

        return scriptPath;
    }

    private enum ScriptState
    {
        Missing,
        Complete,
        Damaged,
    }

    /// <summary>
    /// Compares the file with the expected script. A file that is busy at the moment is retried: the other
    /// party is a scan that has just published it, or a reader, and the state settles within milliseconds.
    /// </summary>
    private static async Task<ScriptState> CheckAsync(string path, byte[] expected, CancellationToken cancellationToken)
    {
        const int attempts = 100;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                // Delete sharing: a check running while another scan publishes must not block that move.
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (stream.Length != expected.Length)
                    return ScriptState.Damaged;

                var actual = new byte[expected.Length];
                await stream.ReadExactlyAsync(actual, cancellationToken);
                return actual.AsSpan().SequenceEqual(expected) ? ScriptState.Complete : ScriptState.Damaged;
            }
            catch (FileNotFoundException)
            {
                return ScriptState.Missing;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && attempt < attempts)
            {
                await Task.Delay(10, cancellationToken);
            }
        }
    }
}
