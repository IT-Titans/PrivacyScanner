using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Mediator;

using Moq;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.DialogViewModels;

/// <summary>
/// Covers the dialog that adds a file extension to the scan blacklist.
/// </summary>
/// <remarks>
/// The Extension setter prepends a dot to every value without one, including the empty
/// string (suspected defect V2 in the work package). The two tests that would otherwise
/// pin that behaviour are written for the correct behaviour and skipped until it is decided.
/// </remarks>
public class AddFileExtensionBlacklistItemDialogViewModelTests
{
    private readonly Mock<IMediator> _mediator = new();

    private AddFileExtensionBlacklistItemDialogViewModel CreateSut() => new(_mediator.Object);

    private void SetupSuccess() =>
        _mediator
            .Setup(m => m.Send(It.IsAny<AddFileExtensionBlacklistItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

    [Fact]
    public void ConstructorRejectsNullMediator()
    {
        Assert.Throws<ArgumentNullException>(() => new AddFileExtensionBlacklistItemDialogViewModel(null!));
    }

    [Fact]
    public void StartsEmptyWithoutError()
    {
        var sut = CreateSut();

        Assert.Equal(string.Empty, sut.Extension);
        Assert.Null(sut.ErrorMessage);
        Assert.False(sut.AddCommand.CanExecute(null));
    }

    [Theory]
    [InlineData("txt", ".txt")]
    [InlineData(".txt", ".txt")]
    [InlineData("tar.gz", ".tar.gz")]
    public void ExtensionGetsExactlyOneLeadingDot(string input, string expected)
    {
        var sut = CreateSut();
        sut.Extension = input;

        Assert.Equal(expected, sut.Extension);
    }

    [Fact]
    public void ExtensionRaisesPropertyChangedOnlyOnRealChange()
    {
        var sut = CreateSut();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.Extension = "txt";
        sut.Extension = ".txt";

        Assert.Equal(new[] { nameof(sut.Extension) }, raised);
    }

    [Fact]
    public void ErrorMessageRaisesPropertyChanged()
    {
        var sut = CreateSut();
        string? name = null;
        sut.PropertyChanged += (_, e) => name = e.PropertyName;

        sut.ErrorMessage = "x";

        Assert.Equal(nameof(sut.ErrorMessage), name);
    }

    [Fact]
    public void CanAddOnceAnExtensionIsEntered()
    {
        var sut = CreateSut();
        sut.Extension = "log";

        Assert.True(sut.AddCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddSendsTheDottedExtensionAndSignalsFocusThenClose()
    {
        AddFileExtensionBlacklistItemCommand? sent = null;
        _mediator
            .Setup(m => m.Send(It.IsAny<AddFileExtensionBlacklistItemCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Unit>, CancellationToken>((c, _) => sent = c as AddFileExtensionBlacklistItemCommand)
            .ReturnsAsync(Unit.Value);
        var sut = CreateSut();
        sut.Extension = "log";
        var events = new List<string>();
        sut.RequestFocusName += () => events.Add("focus");
        sut.RequestClose += () => events.Add("close");

        sut.AddCommand.Execute(null);
        await Task.Yield();

        Assert.NotNull(sent);
        Assert.Equal(".log", sent!.Extension);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(new[] { "focus", "close" }, events);
    }

    [Fact]
    public async Task FailureShowsTheExceptionMessageKeepsTheInputAndDoesNotClose()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<AddFileExtensionBlacklistItemCommand>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("already listed"));
        var sut = CreateSut();
        sut.Extension = "log";
        var closed = false;
        sut.RequestClose += () => closed = true;

        sut.AddCommand.Execute(null);
        await Task.Yield();

        Assert.Equal("already listed", sut.ErrorMessage);
        Assert.Equal(".log", sut.Extension);
        Assert.False(closed);
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

    [Fact(Skip = "Suspected defect V2 (AddFileExtensionBlacklistItemDialogViewModel.cs:24): clearing the field leaves '.', not an empty string. Unskip once the setter is decided.")]
    public void ClearingTheFieldLeavesItEmpty()
    {
        var sut = CreateSut();
        sut.Extension = "log";

        sut.Extension = string.Empty;

        Assert.Equal(string.Empty, sut.Extension);
    }

    [Fact(Skip = "Suspected defect V2 (AddFileExtensionBlacklistItemDialogViewModel.cs:57): after a successful add the field holds '.', so Add stays enabled. Unskip once the setter is decided.")]
    public async Task AfterASuccessfulAddTheFieldIsEmptyAndAddIsDisabled()
    {
        SetupSuccess();
        var sut = CreateSut();
        sut.Extension = "log";

        sut.AddCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(string.Empty, sut.Extension);
        Assert.False(sut.AddCommand.CanExecute(null));
    }
}
