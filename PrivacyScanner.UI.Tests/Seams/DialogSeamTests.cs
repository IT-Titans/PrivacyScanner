using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.Models;
using ITTitans.PrivacyScanner.UI.Services;
using ITTitans.PrivacyScanner.UI.Tests.TestSupport;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Mediator;

using Moq;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.Seams;

/// <summary>
/// Proves that the view models ask the dialog service to show and close dialogs, and no longer build
/// controls or call the dialog host themselves.
/// </summary>
public class DialogSeamTests
{
    [Fact]
    public void OpeningTheSettingsShowsTheSettingsViewModelInTheRootDialog()
    {
        var harness = new MainViewModelHarness();

        harness.ViewModel.OpenSettingsCommand.Execute(null);

        harness.Dialogs.Verify(d => d.ShowDialogAsync(It.IsAny<SettingsDialogViewModel>(), "RootDialog"), Times.Once);
    }

    [Fact]
    public void AddingARegexShowsItsViewModelInTheRootDialog()
    {
        var harness = new MainViewModelHarness();

        harness.ViewModel.AddNewRegexCommand.Execute(null);

        harness.Dialogs.Verify(d => d.ShowDialogAsync(It.IsAny<AddRegexDialogViewModel>(), "RootDialog"), Times.Once);
    }

    [Fact]
    public void AddingADirectoryBlacklistItemShowsItsViewModelInTheRootDialog()
    {
        var harness = new MainViewModelHarness();

        harness.ViewModel.AddNewDirectoryBacklistItemCommand.Execute(null);

        harness.Dialogs.Verify(d => d.ShowDialogAsync(It.IsAny<AddDirectoryBlacklistItemDialogViewModel>(), "RootDialog"), Times.Once);
    }

    [Fact]
    public void AddingAFileExtensionBlacklistItemShowsItsViewModelInTheRootDialog()
    {
        var harness = new MainViewModelHarness();

        harness.ViewModel.AddNewFileExtensionBacklistItemCommand.Execute(null);

        harness.Dialogs.Verify(d => d.ShowDialogAsync(It.IsAny<AddFileExtensionBlacklistItemDialogViewModel>(), "RootDialog"), Times.Once);
    }

    [Fact]
    public void ADialogThatAsksToCloseIsClosedThroughTheDialogService()
    {
        var harness = new MainViewModelHarness();
        harness.Dialogs
            .Setup(d => d.ShowDialogAsync(It.IsAny<SettingsDialogViewModel>(), "RootDialog"))
            .Callback((object viewModel, string _) => ((SettingsDialogViewModel)viewModel).CloseCommand.Execute(null))
            .Returns(Task.CompletedTask);

        harness.ViewModel.OpenSettingsCommand.Execute(null);

        harness.Dialogs.Verify(d => d.CloseDialog("RootDialog"), Times.Once);
    }

    [Fact]
    public void EditingARuleShowsTheEditViewModelInTheSettingsDialogHostAndClosesItThere()
    {
        var dialogs = new Mock<IDialogService>();
        dialogs
            .Setup(d => d.ShowDialogAsync(It.IsAny<EditRegexRuleDialogViewModel>(), "SettingsDialogHost"))
            .Callback((object viewModel, string _) => ((EditRegexRuleDialogViewModel)viewModel).CancelCommand.Execute(null))
            .Returns(Task.CompletedTask);
        var sut = new SettingsDialogViewModel(MediatorWithNoRules().Object, dialogs.Object);

        sut.EditRuleCommand.Execute(new RegexRule { RuleId = Guid.NewGuid(), RuleName = "IBAN", Rule = "x" });

        dialogs.Verify(d => d.ShowDialogAsync(It.IsAny<EditRegexRuleDialogViewModel>(), "SettingsDialogHost"), Times.Once);
        dialogs.Verify(d => d.CloseDialog("SettingsDialogHost"), Times.Once);
    }

    [Fact]
    public void AFailedDeleteIsReportedThroughTheDialogService()
    {
        var mediator = MediatorWithNoRules();
        mediator
            .Setup(m => m.Send(It.IsAny<DeleteRegexRuleCommand>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<DeleteRegexRuleCommandResult>(DeleteRegexRuleCommandResult.Failure("gesperrt")));
        var dialogs = new Mock<IDialogService>();
        dialogs
            .Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), "SettingsDialogHost"))
            .ReturnsAsync(true);
        var sut = new SettingsDialogViewModel(mediator.Object, dialogs.Object);

        sut.DeleteRuleCommand.Execute(new RegexRule { RuleId = Guid.NewGuid(), RuleName = "IBAN", Rule = "x" });

        // Text and title are the ones the message box showed before the seam existed.
        dialogs.Verify(d => d.ShowError("Fehler beim Löschen der Regel: gesperrt", "Fehler"), Times.Once);
    }

    private static Mock<IMediator> MediatorWithNoRules()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetAllRegexRulesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<GetAllRegexRulesQueryResult>(
                new GetAllRegexRulesQueryResult { Rules = new List<RegexRuleDto>() }));

        return mediator;
    }
}
