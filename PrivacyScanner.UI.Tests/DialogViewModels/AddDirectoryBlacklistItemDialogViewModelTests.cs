using System.IO;

using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Mediator;

using Moq;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.DialogViewModels;

/// <summary>
/// Covers the dialog that adds a directory name to the scan blacklist.
/// </summary>
/// <remarks>
/// The user may type a path with surrounding slashes. The stored name must be the bare
/// directory name, and a failing mediator must surface as an error, not a crash.
/// </remarks>
public class AddDirectoryBlacklistItemDialogViewModelTests
{
    private readonly Mock<IMediator> _mediator = new();

    private AddDirectoryBlacklistItemDialogViewModel CreateSut() => new(_mediator.Object);

    [Fact]
    public void ConstructorRejectsNullMediator()
    {
        Assert.Throws<ArgumentNullException>(() => new AddDirectoryBlacklistItemDialogViewModel(null!));
    }

    [Fact]
    public void StartsEmptyWithoutError()
    {
        var sut = CreateSut();

        Assert.Equal(string.Empty, sut.DirectoryName);
        Assert.Null(sut.ErrorMessage);
    }

    [Theory]
    [InlineData("node_modules", "node_modules")]
    [InlineData("/node_modules/", "node_modules")]
    [InlineData("\\bin\\", "bin")]
    public void PreviewShowsTheTrimmedNameBetweenSeparators(string input, string expectedName)
    {
        var sut = CreateSut();
        sut.DirectoryName = input;

        var sep = Path.DirectorySeparatorChar;
        Assert.Equal($"{sep}{expectedName}{sep}", sut.DirectoryNamePreview);
    }

    [Fact]
    public void ChangingTheNameRaisesPreviewChangedAndSameValueDoesNot()
    {
        var sut = CreateSut();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.DirectoryName = "bin";
        sut.DirectoryName = "bin";

        Assert.Equal(new[] { nameof(sut.DirectoryName), nameof(sut.DirectoryNamePreview) }, raised);
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CannotAddABlankName(string name)
    {
        var sut = CreateSut();
        sut.DirectoryName = name;

        Assert.False(sut.AddCommand.CanExecute(null));
    }

    [Fact]
    public void CanAddANonBlankName()
    {
        var sut = CreateSut();
        sut.DirectoryName = "bin";

        Assert.True(sut.AddCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddSendsTheTrimmedNameClearsTheFieldAndSignalsFocusThenClose()
    {
        AddDirectoryBlacklistItemCommand? sent = null;
        _mediator
            .Setup(m => m.Send(It.IsAny<AddDirectoryBlacklistItemCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Unit>, CancellationToken>((c, _) => sent = c as AddDirectoryBlacklistItemCommand)
            .ReturnsAsync(Unit.Value);
        var sut = CreateSut();
        sut.DirectoryName = "/node_modules/";
        var events = new List<string>();
        sut.RequestFocusName += () => events.Add("focus");
        sut.RequestClose += () => events.Add("close");

        sut.AddCommand.Execute(null);
        await Task.Yield();

        Assert.NotNull(sent);
        Assert.Equal("node_modules", sent!.DirectoryName);
        Assert.Equal(string.Empty, sut.DirectoryName);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(new[] { "focus", "close" }, events);
    }

    [Fact]
    public async Task FailureShowsTheExceptionMessageKeepsTheInputAndDoesNotClose()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<AddDirectoryBlacklistItemCommand>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("disk full"));
        var sut = CreateSut();
        sut.DirectoryName = "bin";
        var closed = false;
        sut.RequestClose += () => closed = true;

        sut.AddCommand.Execute(null);
        await Task.Yield();

        Assert.Equal("disk full", sut.ErrorMessage);
        Assert.Equal("bin", sut.DirectoryName);
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
}
