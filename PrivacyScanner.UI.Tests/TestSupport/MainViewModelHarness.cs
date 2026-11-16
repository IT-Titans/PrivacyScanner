using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.Services;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Mediator;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ITTitans.PrivacyScanner.UI.Tests.TestSupport;

/// <summary>
/// Builds a <see cref="MainViewModel"/> with every collaborator replaced, and keeps the replacements
/// reachable so a test can arrange and verify them.
/// </summary>
/// <remarks>
/// The mediator answers the three queries the view model sends while it loads with empty results. A
/// mock without those answers returns null where the view model expects lists. Tests that need other
/// data set the query up again; the later setup wins.
/// </remarks>
internal sealed class MainViewModelHarness
{
    public MainViewModelHarness()
    {
        Mediator
            .Setup(m => m.Send(It.IsAny<CheckPythonAndSpacyEnvironmentQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<CheckPythonAndSpacyEnvironmentQueryResult>(
                CheckPythonAndSpacyEnvironmentQueryResult.Failure("Python is not part of this test.")));

        Mediator
            .Setup(m => m.Send(It.IsAny<GetAllRegexRulesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<GetAllRegexRulesQueryResult>(
                new GetAllRegexRulesQueryResult { Rules = new List<RegexRuleDto>() }));

        Mediator
            .Setup(m => m.Send(It.IsAny<GetAllBlacklistItemsQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<GetAllBlacklistItemsQueryResult>(
                new GetAllBlacklistItemsQueryResult
                {
                    DirectoryBlacklistItems = new List<DirectoryBlacklistItemDto>(),
                    FileExtensionBlacklistItems = new List<FileExtensionBlacklistItemDto>()
                }));

        ViewModel = new MainViewModel(
            Mediator.Object,
            NullLogger<MainViewModel>.Instance,
            Dialogs.Object,
            CsvExport.Object,
            Dispatcher,
            FilePicker.Object,
            FileLauncher.Object);
    }

    public Mock<IMediator> Mediator { get; } = new();

    public Mock<IDialogService> Dialogs { get; } = new();

    public Mock<ICsvExportService> CsvExport { get; } = new();

    public Mock<IFilePickerService> FilePicker { get; } = new();

    public Mock<IFileLauncher> FileLauncher { get; } = new();

    public TestUiDispatcher Dispatcher { get; } = new();

    public MainViewModel ViewModel { get; }
}
