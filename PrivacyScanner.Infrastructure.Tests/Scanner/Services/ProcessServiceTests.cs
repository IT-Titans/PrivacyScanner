using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Services;

/// <summary>
/// Runs the real <see cref="ProcessService"/> against <c>cmd.exe</c>, which exists on every Windows machine
/// the application targets. Tests that need it return early on other operating systems, where there is no such shell.
/// </summary>
public class ProcessServiceTests
{
    private readonly ProcessService _sut = new();

    [Fact]
    public async Task RunCommandAsync_SuccessfulCommand_ReturnsExitCodeZeroAndStandardOutput()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var (exitCode, output, error) = await _sut.RunCommandAsync("cmd.exe", "/c echo hello");

        Assert.Equal(0, exitCode);
        Assert.Equal("hello", output.Trim());
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public async Task RunCommandAsync_NonZeroExit_ReturnsThatExitCode()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var (exitCode, output, _) = await _sut.RunCommandAsync("cmd.exe", "/c exit 3");

        Assert.Equal(3, exitCode);
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public async Task RunCommandAsync_WriteToStandardError_IsCapturedSeparatelyFromOutput()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var (exitCode, output, error) = await _sut.RunCommandAsync("cmd.exe", "/c \"echo oops 1>&2\"");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, output);
        Assert.Equal("oops", error.Trim());
    }

    [Fact]
    public async Task RunCommandAsync_ExecutableDoesNotExist_ReturnsMinusOneWithErrorMessageInsteadOfThrowing()
    {
        var missing = $"privacyscanner-no-such-executable-{Guid.NewGuid():N}";

        var (exitCode, output, error) = await _sut.RunCommandAsync(missing, string.Empty);

        Assert.Equal(-1, exitCode);
        Assert.Equal(string.Empty, output);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
