using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Commands;

/// <summary>
/// Many scans starting at the same instant must all reach Python with a complete script. Before the script was
/// published under a content-derived name by atomic rename, concurrent extraction to one fixed path made the
/// losers fail with a sharing violation, and the handler turned that into an empty result.
/// </summary>
public class SpaCyScanCommandHandlerConcurrencyTests : IDisposable
{
    private const int Rounds = 25;
    private const int ParallelScans = 16;
    private const string OneEntity = "{\"entities\": [{\"text\": \"Max\", \"label\": \"PER\", \"hit_line_position\": 1}]}";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"privacyscanner-spacy-concurrency-{Guid.NewGuid()}");

    public SpaCyScanCommandHandlerConcurrencyTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task Handle_ManyConcurrentScans_EveryScanGetsItsHits()
    {
        var input = Path.Combine(_directory, "input.txt");
        await File.WriteAllTextAsync(input, "Max Mustermann");
        var processService = new Mock<IProcessService>();
        processService.Setup(p => p.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((0, OneEntity, ""));
        var sut = new SpaCyScanCommandHandler(processService.Object, Mock.Of<ILogger<SpaCyScanCommandHandler>>());
        var command = new SpaCyScanCommand { FilePath = new FileInfo(input) };

        var scansWithoutHits = 0;
        for (var round = 0; round < Rounds; round++)
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var scans = Enumerable.Range(0, ParallelScans).Select(async _ =>
            {
                await gate.Task;
                return await sut.Handle(command, CancellationToken.None);
            }).ToArray();
            gate.SetResult();

            foreach (var result in await Task.WhenAll(scans))
            {
                if (result.Warnings.Count == 0)
                {
                    scansWithoutHits++;
                }
            }
        }

        Assert.Equal(0, scansWithoutHits);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
