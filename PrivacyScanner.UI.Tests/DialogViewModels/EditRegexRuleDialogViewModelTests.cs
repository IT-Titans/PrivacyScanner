using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.Models;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Mediator;

using Moq;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.DialogViewModels;

/// <summary>
/// Covers the dialog that edits an existing regex rule.
/// </summary>
/// <remarks>
/// The dialog starts from the rule it was given, must address the update to that rule's
/// id, and stays open with the error shown when the update is rejected.
/// </remarks>
public class EditRegexRuleDialogViewModelTests
{
    private static readonly Guid RuleId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private readonly Mock<IMediator> _mediator = new();

    private static RegexRule Rule() => new() { RuleId = RuleId, RuleName = "Digits", Rule = "\\d+" };

    private EditRegexRuleDialogViewModel CreateSut() => new(_mediator.Object, Rule());

    private void SetupResult(EditRegexRuleCommandResult result) =>
        _mediator
            .Setup(m => m.Send(It.IsAny<EditRegexRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    [Fact]
    public void ConstructorRejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new EditRegexRuleDialogViewModel(null!, Rule()));
        Assert.Throws<ArgumentNullException>(() => new EditRegexRuleDialogViewModel(_mediator.Object, null!));
    }

    [Fact]
    public void StartsWithNameAndPatternOfTheGivenRule()
    {
        var sut = CreateSut();

        Assert.Equal("Digits", sut.RuleName);
        Assert.Equal("\\d+", sut.Regex);
        Assert.Null(sut.ErrorMessage);
    }

    [Theory]
    [InlineData("", "x")]
    [InlineData("  ", "x")]
    [InlineData("x", "")]
    [InlineData("x", "  ")]
    public void CannotUpdateWhileNameOrPatternIsBlank(string name, string regex)
    {
        var sut = CreateSut();
        sut.RuleName = name;
        sut.Regex = regex;

        Assert.False(sut.UpdateCommand.CanExecute(null));
    }

    [Fact]
    public void CanUpdateWithTheInitialValues()
    {
        Assert.True(CreateSut().UpdateCommand.CanExecute(null));
    }

    [Fact]
    public void PropertiesRaisePropertyChangedOnlyOnRealChange()
    {
        var sut = CreateSut();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.RuleName = "Digits";
        sut.RuleName = "Numbers";
        sut.Regex = "\\d";
        sut.ErrorMessage = "e";

        Assert.Equal(new[] { nameof(sut.RuleName), nameof(sut.Regex), nameof(sut.ErrorMessage) }, raised);
    }

    [Fact]
    public async Task SuccessfulUpdateSendsEditedValuesForTheRuleIdAndCloses()
    {
        SetupResult(EditRegexRuleCommandResult.Success());
        var sut = CreateSut();
        sut.RuleName = "Numbers";
        sut.Regex = "\\d{3}";
        var closed = 0;
        sut.RequestClose += () => closed++;

        sut.UpdateCommand.Execute(null);
        await Task.Yield();

        _mediator.Verify(m => m.Send(
            It.Is<EditRegexRuleCommand>(c => c.RuleId == RuleId && c.RuleName == "Numbers" && c.Rule == "\\d{3}"),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(1, closed);
    }

    [Fact]
    public async Task RejectedUpdateShowsErrorAndDoesNotClose()
    {
        SetupResult(EditRegexRuleCommandResult.Failure("rule not found"));
        var sut = CreateSut();
        var closed = false;
        sut.RequestClose += () => closed = true;

        sut.UpdateCommand.Execute(null);
        await Task.Yield();

        Assert.Equal("rule not found", sut.ErrorMessage);
        Assert.False(closed);
        Assert.Equal("Digits", sut.RuleName);
    }

    [Fact]
    public async Task PreviousErrorIsClearedBeforeANewAttempt()
    {
        SetupResult(EditRegexRuleCommandResult.Success());
        var sut = CreateSut();
        sut.ErrorMessage = "old";

        sut.UpdateCommand.Execute(null);
        await Task.Yield();

        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public void CancelClosesWithoutCallingTheMediator()
    {
        var sut = CreateSut();
        var closed = 0;
        sut.RequestClose += () => closed++;

        sut.CancelCommand.Execute(null);

        Assert.Equal(1, closed);
        _mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public void CancelWithoutSubscriberDoesNotThrow()
    {
        CreateSut().CancelCommand.Execute(null);
    }
}
