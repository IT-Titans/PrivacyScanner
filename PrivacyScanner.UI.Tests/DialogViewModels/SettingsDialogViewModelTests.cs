using System.Collections.ObjectModel;

using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.Models;
using ITTitans.PrivacyScanner.UI.Services;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Mediator;

using Moq;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.DialogViewModels;

/// <summary>
/// Covers the settings dialog that lists, deletes and (partly) edits regex rules.
/// </summary>
/// <remarks>
/// Not covered on main without production seams: the failure branch of delete (static
/// MessageBox.Show) and the edit flow (new Controls.EditRegexRuleDialog and the static
/// DialogHost). Both need a WPF window, so they wait for the dialog-service seam.
/// </remarks>
public class SettingsDialogViewModelTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IDialogService> _dialogs = new();

    private SettingsDialogViewModel CreateSut() => new(_mediator.Object, _dialogs.Object);

    private static RegexRule Rule(string name) => new() { RuleId = Guid.NewGuid(), RuleName = name, Rule = name + ".*" };

    private void SetupConfirmation(bool answer) =>
        _dialogs
            .Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(answer);

    [Fact]
    public void ConstructorRejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new SettingsDialogViewModel(null!, _dialogs.Object));
        Assert.Throws<ArgumentNullException>(() => new SettingsDialogViewModel(_mediator.Object, null!));
    }

    [Fact]
    public void StartsWithNoRules()
    {
        Assert.Empty(CreateSut().Rules);
    }

    [Fact]
    public void ReplacingRulesRaisesPropertyChanged()
    {
        var sut = CreateSut();
        string? name = null;
        sut.PropertyChanged += (_, e) => name = e.PropertyName;
        var replacement = new ObservableCollection<RegexRule>();

        sut.Rules = replacement;

        Assert.Equal(nameof(sut.Rules), name);
        Assert.Same(replacement, sut.Rules);
    }

    [Fact]
    public async Task InitializeLoadsAllRulesFromTheMediatorWithTheirFields()
    {
        var first = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "Mail", Rule = "@" };
        var second = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "IBAN", Rule = "DE\\d+" };
        _mediator
            .Setup(m => m.Send(It.IsAny<GetAllRegexRulesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAllRegexRulesQueryResult { Rules = [first, second] });
        var sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal(2, sut.Rules.Count);
        Assert.Equal(first.RuleId, sut.Rules[0].RuleId);
        Assert.Equal("Mail", sut.Rules[0].RuleName);
        Assert.Equal("@", sut.Rules[0].Rule);
        Assert.Equal(second.RuleId, sut.Rules[1].RuleId);
        Assert.Equal("DE\\d+", sut.Rules[1].Rule);
    }

    [Fact]
    public async Task InitializeReplacesRulesThatWereLoadedBefore()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<GetAllRegexRulesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAllRegexRulesQueryResult
            {
                Rules = [new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "Only", Rule = "x" }],
            });
        var sut = CreateSut();
        sut.Rules.Add(Rule("stale"));

        await sut.InitializeAsync();

        var single = Assert.Single(sut.Rules);
        Assert.Equal("Only", single.RuleName);
    }

    [Fact]
    public void DeleteWithoutARuleDoesNothing()
    {
        var sut = CreateSut();

        sut.DeleteRuleCommand.Execute(null);

        _dialogs.VerifyNoOtherCalls();
        _mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsksForConfirmationNamingTheRule()
    {
        SetupConfirmation(false);
        var sut = CreateSut();

        sut.DeleteRuleCommand.Execute(Rule("Mail"));
        await Task.Yield();

        _dialogs.Verify(d => d.ShowConfirmationAsync(
            It.IsAny<string>(),
            It.Is<string>(m => m.Contains("'Mail'")),
            "SettingsDialogHost"), Times.Once);
    }

    [Fact]
    public async Task DeclinedConfirmationKeepsTheRuleAndSendsNothing()
    {
        SetupConfirmation(false);
        var sut = CreateSut();
        var rule = Rule("Mail");
        sut.Rules.Add(rule);

        sut.DeleteRuleCommand.Execute(rule);
        await Task.Yield();

        Assert.Contains(rule, sut.Rules);
        _mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ConfirmedDeleteSendsTheRuleIdAndRemovesTheRule()
    {
        SetupConfirmation(true);
        _mediator
            .Setup(m => m.Send(It.IsAny<DeleteRegexRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DeleteRegexRuleCommandResult.Success());
        var sut = CreateSut();
        var doomed = Rule("Mail");
        var kept = Rule("IBAN");
        sut.Rules.Add(doomed);
        sut.Rules.Add(kept);

        sut.DeleteRuleCommand.Execute(doomed);
        await Task.Yield();

        _mediator.Verify(m => m.Send(
            It.Is<DeleteRegexRuleCommand>(c => c.RuleId == doomed.RuleId),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.DoesNotContain(doomed, sut.Rules);
        Assert.Contains(kept, sut.Rules);
    }

    [Fact]
    public void EditWithoutARuleDoesNothing()
    {
        var sut = CreateSut();

        sut.EditRuleCommand.Execute(null);

        _dialogs.VerifyNoOtherCalls();
        _mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public void CloseRaisesRequestClose()
    {
        var sut = CreateSut();
        var closed = 0;
        sut.RequestClose += () => closed++;

        sut.CloseCommand.Execute(null);

        Assert.Equal(1, closed);
    }

    [Fact]
    public void CloseWithoutSubscriberDoesNotThrow()
    {
        CreateSut().CloseCommand.Execute(null);
    }
}
