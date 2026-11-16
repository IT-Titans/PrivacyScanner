using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using ITTitans.PrivacyScanner.UI.Services;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.Seams;

/// <summary>
/// Builds the container the application builds and resolves from it.
/// </summary>
/// <remarks>
/// A constructor parameter nobody registered does not fail at compile time and not at start-up either.
/// It fails when the first request reaches the handler that needs it, which for a settings handler is
/// the first click on a rule. Resolving everything here moves that failure into the pipeline.
/// </remarks>
public class ServiceRegistrationTests
{
    [Fact]
    public void EveryRegisteredServiceCanBeResolved()
    {
        var services = new ServiceCollection().AddPrivacyScanner();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        var closedServiceTypes = services
            .Select(descriptor => descriptor.ServiceType)
            .Where(type => !type.IsGenericTypeDefinition)
            .Distinct()
            .ToList();

        Assert.NotEmpty(closedServiceTypes);
        Assert.All(closedServiceTypes, type => Assert.NotNull(provider.GetRequiredService(type)));
    }

    [Fact]
    public void TheContainerHandsOutTheProductionImplementations()
    {
        using var provider = new ServiceCollection().AddPrivacyScanner().BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<MainViewModel>());
        Assert.IsType<DialogService>(provider.GetRequiredService<IDialogService>());
        Assert.IsType<SettingsPathProvider>(provider.GetRequiredService<ISettingsPathProvider>());
    }
}
