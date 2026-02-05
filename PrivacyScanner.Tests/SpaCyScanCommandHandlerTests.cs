using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Tests;

public class SpaCyScanCommandHandlerTests : IDisposable
{
    private readonly Mock<IProcessService> _processServiceMock;
    private readonly SpaCyScanCommandHandler _handler;
    private readonly string _tempFilePath;
    private readonly ILogger<SpaCyScanCommandHandler> _logger;

    public SpaCyScanCommandHandlerTests()
    {
        _logger = LoggerFactory.Create(builder => { }).CreateLogger<SpaCyScanCommandHandler>();
        _processServiceMock = new Mock<IProcessService>();
        _handler = new SpaCyScanCommandHandler(_processServiceMock.Object, _logger);
        _tempFilePath = Path.GetTempFileName();
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public async Task Handle_StandardCase_ReturnsCorrectThreeLineContext()
    {
        // Arrange
        var lines = new[] { "Zeile 1", "Zeile 2 mit Entity", "Zeile 3" };
        await File.WriteAllLinesAsync(_tempFilePath, lines);

        var spacyOutput = CreateSpacyOutput("Entity", 10);

        int startPos = lines[0].Length + Environment.NewLine.Length + 8;

        _processServiceMock.Setup(x => x.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((0, CreateSpacyOutput("Entity", startPos, 2, lines[0], lines[1], lines[2]), ""));

        var command = new SpaCyScanCommand { FilePath = new FileInfo(_tempFilePath) };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Single(result.Warnings);
        var warning = result.Warnings[0];
        Assert.Equal(2, warning.Line);
        Assert.Equal(17, warning.Start);
        Assert.Equal(lines[0], warning.SuspiciousContent.PrevLine);
        Assert.Equal(lines[1], warning.SuspiciousContent.HitLine);
        Assert.Equal(lines[2], warning.SuspiciousContent.NextLine);
        Assert.Equal(ScanWarningType.SpaCy, warning.Type);
        Assert.Null(warning.RuleId);
    }

    [Fact]
    public async Task Handle_FirstLineEntity_ReturnsTwoLineContext()
    {
        // Arrange
        var lines = new[] { "Entity in Zeile 1", "Zeile 2", "Zeile 3" };
        await File.WriteAllLinesAsync(_tempFilePath, lines);

        _processServiceMock.Setup(x => x.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((0, CreateSpacyOutput("Entity", 0, 1, string.Empty, lines[0], lines[1]), ""));

        var command = new SpaCyScanCommand { FilePath = new FileInfo(_tempFilePath) };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Single(result.Warnings);
        var warning = result.Warnings[0];
        Assert.Equal(1, warning.Line);
        Assert.Equal(string.Empty, warning.SuspiciousContent.PrevLine);
        Assert.Equal(lines[0], warning.SuspiciousContent.HitLine);
        Assert.Equal(lines[1], warning.SuspiciousContent.NextLine);
    }

    [Fact]
    public async Task Handle_LastLineEntity_ReturnsTwoLineContext()
    {
        // Arrange
        var lines = new[] { "Zeile 1", "Zeile 2", "Entity in Zeile 3" };
        await File.WriteAllLinesAsync(_tempFilePath, lines);

        int startPos = (lines[0].Length + Environment.NewLine.Length) + (lines[1].Length + Environment.NewLine.Length);

        _processServiceMock.Setup(x => x.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((0, CreateSpacyOutput("Entity", startPos, 3, lines[1], lines[2], string.Empty), ""));

        var command = new SpaCyScanCommand { FilePath = new FileInfo(_tempFilePath) };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Single(result.Warnings);
        var warning = result.Warnings[0];
        Assert.Equal(3, warning.Line);
        Assert.Equal(lines[1], warning.SuspiciousContent.PrevLine);
        Assert.Equal(lines[2], warning.SuspiciousContent.HitLine);
        Assert.Equal(string.Empty, warning.SuspiciousContent.NextLine);
    }

    [Fact]
    public async Task Handle_MultipleEntities_ReturnsMultipleWarnings()
    {
        // Arrange
        var lines = new[] { "Entity1", "Zeile 2", "Entity2" };
        await File.WriteAllLinesAsync(_tempFilePath, lines);

        var entities = new[]
        {
            new { text = "Entity1", label = "PER", start = 0, hit_line_position = 1, prev_line = "", hit_line = lines[0], next_line = lines[1] },
            new { text = "Entity2", label = "ORG", start = lines[0].Length + Environment.NewLine.Length + lines[1].Length + Environment.NewLine.Length, hit_line_position = 3, prev_line = lines[1], hit_line = lines[2], next_line = "" }
        };
        var json = JsonSerializer.Serialize(new { entities = entities });

        _processServiceMock.Setup(x => x.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((0, json, ""));

        var command = new SpaCyScanCommand { FilePath = new FileInfo(_tempFilePath) };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Warnings.Count);
        Assert.Equal(1, result.Warnings[0].Line);
        Assert.Equal(3, result.Warnings[1].Line);
    }

    [Fact]
    public async Task Handle_EmptyFile_ReturnsEmptyWarnings()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "");

        _processServiceMock.Setup(x => x.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((0, "{\"entities\": []}", ""));

        var command = new SpaCyScanCommand { FilePath = new FileInfo(_tempFilePath) };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Handle_UnicodeCharacters_CalculatesPositionCorrectly()
    {
        // Arrange
        var lines = new[] { "Überprüfung mit Emoji 🛡️", "Entity hier" };
        await File.WriteAllLinesAsync(_tempFilePath, lines);

        // "Überprüfung mit Emoji 🛡️"
        // Überprüfung = 11
        // Leerzeichen = 1
        // mit = 3
        // Leerzeichen = 1
        // Emoji = 5
        // Leerzeichen = 1
        // 🛡️ = 🛡 (U+1F6E1) + FE0F (Variation Selector) -> In UTF-16 (C# string) sind das Surrogate Pairs.
        // 🛡 ist 2 chars. FE0F ist 1 char. Zusammen 3 chars?

        int line1Length = lines[0].Length;
        int startPos = line1Length + Environment.NewLine.Length + 0; // "Entity" am Anfang von Zeile 2

        _processServiceMock.Setup(x => x.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((0, CreateSpacyOutput("Entity", startPos, 2, lines[0], lines[1], ""), ""));

        var command = new SpaCyScanCommand { FilePath = new FileInfo(_tempFilePath) };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Single(result.Warnings);
        Assert.Equal(2, result.Warnings[0].Line);
        Assert.Equal(27, result.Warnings[0].Start);
    }

    [Fact]
    public async Task Handle_InvalidJson_ReturnsEmptyWarningsAndNoException()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "some text");

        _processServiceMock.Setup(x => x.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((0, "invalid json", ""));

        var command = new SpaCyScanCommand { FilePath = new FileInfo(_tempFilePath) };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Handle_SpaCyNotAvailable_ReturnsEmptyWarnings()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "some text");

        _processServiceMock.Setup(x => x.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((-1, "", "py not found"));

        var command = new SpaCyScanCommand { FilePath = new FileInfo(_tempFilePath) };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Empty(result.Warnings);
    }

    [Fact(Skip = "Integrationstest erfordert lokales Python und spacy_scan.py")]
    public async Task Handle_Integration_WithRealSpaCy_Works()
    {
        // Arrange
        var processService = new ITTitans.PrivacyScanner.Infrastructure.Scanner.Services.ProcessService();
        var handler = new SpaCyScanCommandHandler(processService, _logger);

        var lines = new[] { "Das ist ein Test mit Max Mustermann.", "Zweite Zeile." };
        await File.WriteAllLinesAsync(_tempFilePath, lines);

        var command = new SpaCyScanCommand { FilePath = new FileInfo(_tempFilePath) };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        // Wir können hier nicht hart prüfen, ob es klappt, da SpaCy eventuell nicht installiert ist.
        // Aber wir prüfen, dass keine Exception fliegt.
        Assert.NotNull(result);
    }

    private string CreateSpacyOutput(string text, int start, int line = 0, string prev = "", string hit = "", string next = "")
    {
        var result = new
        {
            entities = new[]
            {
                new {
                    text = text,
                    label = "PER",
                    start = start,
                    hit_line_position = line,
                    prev_line = prev,
                    hit_line = hit,
                    next_line = next
                }
            }
        };
        return JsonSerializer.Serialize(result);
    }
}
