using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;
using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Helpers;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

public class SpaCyScanCommandHandler : IRequestHandler<SpaCyScanCommand, ScanResultDto>
{
    private readonly IProcessService _processService;
    private readonly ILogger<SpaCyScanCommandHandler> _logger;

    public SpaCyScanCommandHandler(IProcessService processService, ILogger<SpaCyScanCommandHandler> logger)
    {
        _processService = processService;
        this._logger = logger;
    }

    public async ValueTask<ScanResultDto> Handle(SpaCyScanCommand request, CancellationToken cancellationToken)
    {
        var warnings = new List<ScanWarningDto>();
        var totalStopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Step 0: Handle gestartet für Datei {File}", request.FilePath);

            // Step 1: Prüfe Datei existiert
            var step1Stopwatch = Stopwatch.StartNew();
            if (!request.FilePath.Exists)
            {
                step1Stopwatch.Stop();
                _logger.LogInformation("Step 1 beendet nach {ElapsedMilliseconds}ms - Datei nicht gefunden", step1Stopwatch.ElapsedMilliseconds);
                totalStopwatch.Stop();
                _logger.LogInformation("Handle insgesamt {ElapsedMilliseconds}ms", totalStopwatch.ElapsedMilliseconds);
                return new ScanResultDto { FilePath = request.FilePath, Warnings = warnings };
            }
            step1Stopwatch.Stop();
            _logger.LogInformation("Step 1 beendet nach {ElapsedMilliseconds}ms - Datei existiert", step1Stopwatch.ElapsedMilliseconds);

            // Step 2: Alle Zeilen lesen
            var step2Stopwatch = Stopwatch.StartNew();
            var allLines = await File.ReadAllLinesAsync(request.FilePath.FullName, cancellationToken);
            step2Stopwatch.Stop();
            _logger.LogInformation("Step 2: Datei gelesen in {ElapsedMilliseconds}ms, Zeilen={LineCount}", step2Stopwatch.ElapsedMilliseconds, allLines.Length);

            // Step 3: Pfad zum SpaCy-Skript abrufen
            string scriptPath = await GetScriptPath(cancellationToken);
            _logger.LogInformation("Step 3: Starte SpaCy Skript unter {ScriptPath}", scriptPath);
            var step3Stopwatch = Stopwatch.StartNew();

            var (exitCode, output, error) = await _processService.RunCommandAsync(
                "py", $"-3.12 \"{scriptPath}\" \"{request.FilePath.FullName}\"");

            step3Stopwatch.Stop();
            _logger.LogInformation("Step 3: SpaCy Skript beendet in {ElapsedMilliseconds}ms, ExitCode={ExitCode}", step3Stopwatch.ElapsedMilliseconds, exitCode);

            if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                _logger.LogWarning("SpaCy-Skript returned empty output or non-zero exit code.");
                totalStopwatch.Stop();
                _logger.LogInformation("Handle insgesamt {ElapsedMilliseconds}ms", totalStopwatch.ElapsedMilliseconds);
                return new ScanResultDto { FilePath = request.FilePath, Warnings = warnings };
            }

            // Step 4: JSON deserialisieren
            _logger.LogInformation("Step 4: JSON deserialisieren");
            var step4Stopwatch = Stopwatch.StartNew();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new SpaCyLabelConverter());

            var spacyResult = JsonSerializer.Deserialize<SpacyResult>(output, options);

            step4Stopwatch.Stop();
            _logger.LogInformation("Step 4: JSON deserialisiert in {ElapsedMilliseconds}ms", step4Stopwatch.ElapsedMilliseconds);

            if (spacyResult?.Entities == null)
            {
                _logger.LogInformation("Keine Entities gefunden.");
                totalStopwatch.Stop();
                _logger.LogInformation("Handle insgesamt {ElapsedMilliseconds}ms", totalStopwatch.ElapsedMilliseconds);
                return new ScanResultDto { FilePath = request.FilePath, Warnings = warnings };
            }

            // Step 5: Entities in Warnings umwandeln
            _logger.LogInformation("Step 5: Entities mappen");
            var step5Stopwatch = Stopwatch.StartNew();

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

            step5Stopwatch.Stop();
            _logger.LogInformation("Step 5: Entities gemappt in {ElapsedMilliseconds}ms, Count={Count}", step5Stopwatch.ElapsedMilliseconds, spacyResult.Entities.Count);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception im SpaCyScanCommandHandler");
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }

        totalStopwatch.Stop();
        _logger.LogInformation("Handle abgeschlossen insgesamt {ElapsedMilliseconds}ms", totalStopwatch.ElapsedMilliseconds);

        return new ScanResultDto
        {
            FilePath = request.FilePath,
            Warnings = warnings
        };
    }


    private async Task<string> GetScriptPath(CancellationToken cancellationToken)
    {
        var assembly = typeof(SpaCyScanCommandHandler).Assembly;
        var assemblyLocation = Path.GetDirectoryName(assembly.Location);

        // Falls wir nicht im SingleFile-Modus sind, könnte die Datei im Resources-Ordner liegen
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var localPath = Path.Combine(assemblyLocation, "Resources", "spacy_scan.py");
            if (File.Exists(localPath)) return localPath;
        }

        // Sonst aus Embedded Resources extrahieren
        var tempPath = Path.Combine(Path.GetTempPath(), "PrivacyScanner");
        Directory.CreateDirectory(tempPath);
        var scriptPath = Path.Combine(tempPath, "spacy_scan.py");

        // Immer extrahieren, um sicherzustellen, dass wir die aktuellste Version haben
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



