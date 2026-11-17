using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Mediator;

using Moq;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.DialogViewModels;

/// <summary>
/// Covers the dialog that creates a user-defined regex rule.
/// </summary>
/// <remarks>
/// The dialog must hand exactly what the user typed to the mediator, keep itself open
/// with the error shown when the rule is rejected, and close only on success.
/// </remarks>
public class AddRegexDialogViewModelTests
{
    private readonly Mock<IMediator> _mediator = new();

    private AddRegexDialogViewModel CreateSut() => new(_mediator.Object);

    private void SetupResult(AddRegexRuleCommandResult result) =>
        _mediator
            .Setup(m => m.Send(It.IsAny<AddRegexRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    [Fact]
    public void ConstructorRejectsNullMediator()
    {
        Assert.Throws<ArgumentNullException>(() => new AddRegexDialogViewModel(null!));
    }

    [Fact]
    public void StartsEmptyWithoutError()
    {
        var sut = CreateSut();

        Assert.Equal(string.Empty, sut.RuleName);
        Assert.Equal(string.Empty, sut.Regex);
        Assert.Null(sut.ErrorMessage);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("name", "")]
    [InlineData("", "\\d+")]
    [InlineData("   ", "\\d+")]
    [InlineData("name", "   ")]
    public void CannotAddWhileNameOrPatternIsBlank(string name, string regex)
    {
        var sut = CreateSut();
        sut.RuleName = name;
        sut.Regex = regex;

        Assert.False(sut.AddCommand.CanExecute(null));
    }

    [Fact]
    public void CanAddWhenNameAndPatternAreFilled()
    {
        var sut = CreateSut();
        sut.RuleName = "Digits";
        sut.Regex = "\\d+";

        Assert.True(sut.AddCommand.CanExecute(null));
    }

    [Fact]
    public void PropertiesRaisePropertyChangedOnlyOnRealChange()
    {
        var sut = CreateSut();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.RuleName = "a";
        sut.RuleName = "a";
        sut.Regex = "b";
        sut.ErrorMessage = "c";

        Assert.Equal(new[] { nameof(sut.RuleName), nameof(sut.Regex), nameof(sut.ErrorMessage) }, raised);
    }

    [Fact]
    public async Task SuccessfulAddSendsCommandClearsFieldsAndFocusesThenCloses()
    {
        SetupResult(AddRegexRuleCommandResult.Success());
        var sut = CreateSut();
        sut.RuleName = "Digits";
        sut.Regex = "\\d+";
        var events = new List<string>();
        sut.RequestFocusName += () => events.Add("focus");
        sut.RequestClose += () => events.Add("close");

        sut.AddCommand.Execute(null);
        await Task.Yield();

        _mediator.Verify(m => m.Send(
            It.Is<AddRegexRuleCommand>(c => c.RuleName == "Digits" && c.Rule == "\\d+"),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, sut.RuleName);
        Assert.Equal(string.Empty, sut.Regex);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(new[] { "focus", "close" }, events);
    }

    [Fact]
    public async Task RejectedRuleShowsErrorKeepsInputAndDoesNotClose()
    {
        SetupResult(AddRegexRuleCommandResult.Failure("invalid pattern"));
        var sut = CreateSut();
        sut.RuleName = "Broken";
        sut.Regex = "(";
        var closed = false;
        var focused = false;
        sut.RequestClose += () => closed = true;
        sut.RequestFocusName += () => focused = true;

        sut.AddCommand.Execute(null);
        await Task.Yield();

        Assert.Equal("invalid pattern", sut.ErrorMessage);
        Assert.Equal("Broken", sut.RuleName);
        Assert.Equal("(", sut.Regex);
        Assert.False(closed);
        Assert.False(focused);
    }

    [Fact]
    public async Task PreviousErrorIsClearedBeforeANewAttempt()
    {
        SetupResult(AddRegexRuleCommandResult.Success());
        var sut = CreateSut();
        sut.RuleName = "n";
        sut.Regex = "r";
        sut.ErrorMessage = "old error";

        sut.AddCommand.Execute(null);
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
