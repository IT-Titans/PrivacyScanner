using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.Models;
using ITTitans.PrivacyScanner.UI.Tests.MainViewModelState;
using ITTitans.PrivacyScanner.UI.Tests.TestSupport;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Moq;

using Xunit;

using static ITTitans.PrivacyScanner.UI.Tests.MainViewModelState.MainViewModelStateTestData;

namespace ITTitans.PrivacyScanner.UI.Tests.Views;

/// <summary>
/// Drives a real <see cref="ItemsControl"/> with the rule chip template of the main window, without a
/// window: the control is measured and arranged on its own, and the automation peer of the text is read directly.
/// The peer of the items control itself is not asserted: it rebuilds its children from a queued dispatcher
/// operation that a headless test cannot trigger without reaching into WPF internals.
/// </summary>
public class RuleChipsAutomationTests
{
    // Same bindings as the chip template in MainWindow.xaml, minus the converters and the icon, which
    // do not take part in what UI Automation reads.
    private const string ChipTemplate = """
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding RuleName}" />
                <Button Command="{Binding DataContext.ToggleRuleCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                        CommandParameter="{Binding}" />
            </StackPanel>
        </DataTemplate>
        """;

    private const string ControlTemplateXaml = """
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="ItemsControl">
            <ItemsPresenter />
        </ControlTemplate>
        """;

    private static void RunOnStaThread(Func<Task> test)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                test().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static List<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        var found = new List<T>();
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                found.Add(match);
            }
            found.AddRange(Descendants<T>(child));
        }
        return found;
    }

    [Fact]
    public void AfterAnEditAndReloadTheChipShowsAndReadsTheNewRuleAndItsButtonCarriesIt()
    {
        RunOnStaThread(async () =>
        {
            var harness = new MainViewModelHarness();
            var original = Rule("Alt");
            harness.ServeRules(original);
            await harness.ViewModel.InitializeAsync();

            var chips = new ItemsControl
            {
                DataContext = harness.ViewModel,
                // An explicit template keeps the test independent of the theme dictionaries.
                Template = (ControlTemplate)XamlReader.Parse(ControlTemplateXaml),
                ItemTemplate = (DataTemplate)XamlReader.Parse(ChipTemplate)
            };
            chips.SetBinding(ItemsControl.ItemsSourceProperty, nameof(MainViewModel.FilteredRegexRules));
            chips.Measure(new Size(400, 400));
            chips.Arrange(new Rect(0, 0, 400, 400));
            chips.UpdateLayout();

            // The settings dialog edits the rule; closing it reloads the rules, same id and a new name.
            harness.Dialogs
                .Setup(d => d.ShowDialogAsync(It.IsAny<SettingsDialogViewModel>(), "RootDialog"))
                .Callback(() => harness.ServeRules(new RegexRuleDto { RuleId = original.RuleId, RuleName = "Neu", Rule = "y+", IsEnabled = true }))
                .Returns(Task.CompletedTask);
            harness.ViewModel.OpenSettingsCommand.Execute(null);
            chips.UpdateLayout();

            var text = Assert.Single(Descendants<TextBlock>(chips));
            var button = Assert.Single(Descendants<Button>(chips));
            Assert.Equal("Neu", UIElementAutomationPeer.CreatePeerForElement(text).GetName());
            Assert.Same(harness.ViewModel.FilteredRegexRules[0], button.CommandParameter);
            Assert.Equal("Neu", ((RegexRule)button.CommandParameter).RuleName);
        });
    }
}
