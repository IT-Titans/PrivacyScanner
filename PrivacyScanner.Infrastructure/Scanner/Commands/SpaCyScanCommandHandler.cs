using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Helpers;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

/// <summary>
/// Scans a file for named entities by running the bundled spaCy Python script as a subprocess.
/// </summary>
public class SpaCyScanCommandHandler : IRequestHandler<SpaCyScanCommand, ScanResultDto>
{
    private readonly IProcessService _processService;
    private readonly ILogger _logger;

    public SpaCyScanCommandHandler(IProcessService processService, ILogger<SpaCyScanCommandHandler> logger)
    {
        _processService = processService ?? throw new ArgumentNullException(nameof(processService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<ScanResultDto> Handle(SpaCyScanCommand request, CancellationToken cancellationToken)
    {
        var warnings = new List<ScanWarningDto>();

        try
        {
            // Step 1: Check file exists
            if (!request.FilePath.Exists)
            {
                return new ScanResultDto { FilePath = request.FilePath, Warnings = warnings };
            }

            // Step 2: Read all lines
            var allLines = await File.ReadAllLinesAsync(request.FilePath.FullName, cancellationToken);

            // Step 3: Get path to SpaCy script
            string scriptPath = await GetScriptPathAsync(cancellationToken);

            var (exitCode, output, error) = await _processService.RunCommandAsync(
                "py", $"-3.12 \"{scriptPath}\" \"{request.FilePath.FullName}\"");

            if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                return new ScanResultDto { FilePath = request.FilePath, Warnings = warnings };
            }

            // Step 4: Deserialize JSON
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new SpaCyLabelConverter());

            var spacyResult = JsonSerializer.Deserialize<SpacyResult>(output, options);

            if (spacyResult?.Entities == null)
            {
                return new ScanResultDto { FilePath = request.FilePath, Warnings = warnings };
            }

            // Step 5: Convert entities into warnings

            foreach (var entity in spacyResult.Entities)
            {
                warnings.Add(new ScanWarningDto
                {
                    Line = entity.HitLinePosition,
                    Start = entity.Start,
                    End = entity.End,
                    SuspiciousContent = new SuspiciousContentDto
                    {
                        MatchText = entity.Text,
                        PrevLine = entity.PrevLine,
                        HitLine = entity.HitLine,
                        NextLine = entity.NextLine
                    },
                    Type = ScanWarningType.SpaCy,
                    RuleName = null,
                    RuleId = null,
                    SpacyLabel = entity.Label
                });
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in SpaCyScanCommandHandler");
        }

        return new ScanResultDto
        {
            FilePath = request.FilePath,
            Warnings = warnings
        };
    }


    private async Task<string> GetScriptPathAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(SpaCyScanCommandHandler).Assembly;
        var assemblyLocation = Path.GetDirectoryName(assembly.Location);

        // If not running in single-file mode, the file might be in the Resources folder
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var localPath = Path.Combine(assemblyLocation, "Resources", "spacy_scan.py");
            if (File.Exists(localPath))
                return localPath;
        }

        // Otherwise extract from embedded resources
        var tempPath = Path.Combine(Path.GetTempPath(), "PrivacyScanner");
        Directory.CreateDirectory(tempPath);
        var scriptPath = Path.Combine(tempPath, "spacy_scan.py");

        // Always extract to ensure we have the latest version
        const string resourceName = "ITTitans.PrivacyScanner.Infrastructure.Resources.spacy_scan.py";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            var availableResources = string.Join(", ", assembly.GetManifestResourceNames());
            throw new FileNotFoundException($"Die eingebettete Ressource '{resourceName}' wurde nicht gefunden. Verfügbare Ressourcen: {availableResources}");
        }

        using var fileStream = File.Create(scriptPath);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return scriptPath;
    }

    private class SpacyResult
    {
        [JsonPropertyName("entities")]
        public List<SpacyEntity>? Entities { get; set; }
    }

    private class SpacyEntity
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public SpaCyLabel Label { get; set; } = SpaCyLabel.Unknown;

        [JsonPropertyName("hit_line_position")]
        public int HitLinePosition { get; set; }

        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("end")]
        public int End { get; set; }

        [JsonPropertyName("prev_line")]
        public string PrevLine { get; set; } = string.Empty;

        [JsonPropertyName("hit_line")]
        public string HitLine { get; set; } = string.Empty;

        [JsonPropertyName("next_line")]
        public string NextLine { get; set; } = string.Empty;
    }

}



