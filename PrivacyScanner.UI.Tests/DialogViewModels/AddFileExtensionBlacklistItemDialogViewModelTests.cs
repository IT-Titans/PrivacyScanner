using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Mediator;

using Moq;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.DialogViewModels;

/// <summary>
/// Covers the dialog that adds a file extension to the scan blacklist.
/// </summary>
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

    [Theory]
    [InlineData("..log", ".log")]
    [InlineData("...tar.gz", ".tar.gz")]
    [InlineData("  log  ", ".log")]
    [InlineData(" .log", ".log")]
    [InlineData(". log", ".log")]
    [InlineData("LOG", ".LOG")]
    [InlineData(".Log", ".Log")]
    public void ExtensionIsTrimmedAndKeepsItsCase(string input, string expected)
    {
        var sut = CreateSut();
        sut.Extension = input;

        Assert.Equal(expected, sut.Extension);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData(" ")]
    [InlineData(" . ")]
    [InlineData("")]
    public void InputWithoutNameStaysEmptyAndCannotBeAdded(string input)
    {
        var sut = CreateSut();
        sut.Extension = input;

        Assert.Equal(string.Empty, sut.Extension);
        Assert.False(sut.AddCommand.CanExecute(null));
    }

    [Fact]
    public void ALoneDotInAnEmptyFieldRaisesPropertyChangedSoTheTextBoxIsReset()
    {
        var sut = CreateSut();
        var raised = 0;
        sut.PropertyChanged += (_, e) => raised += e.PropertyName == nameof(sut.Extension) ? 1 : 0;

        sut.Extension = ".";

        Assert.Equal(1, raised);
    }

    [Fact]
    public void TypingADotThenTheNameYieldsTheDottedExtension()
    {
        var sut = CreateSut();

        foreach (var typed in new[] { ".", ".l", ".lo", ".log" })
        {
            sut.Extension = typed;
        }

        Assert.Equal(".log", sut.Extension);
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

    [Fact]
    public void ClearingTheFieldLeavesItEmpty()
    {
        var sut = CreateSut();
        sut.Extension = "log";

        sut.Extension = string.Empty;

        Assert.Equal(string.Empty, sut.Extension);
    }

    [Fact]
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
