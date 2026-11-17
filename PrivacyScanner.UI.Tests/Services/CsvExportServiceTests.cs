using System.IO;
using System.Text;

using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.Services;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.Services;

/// <summary>
/// Covers the CSV file written while a scan runs.
/// </summary>
/// <remarks>
/// Formula neutralisation (suspected defect P0-1, CsvExportService.cs:68-79) is deliberately
/// not asserted in either direction; the working package says to leave it out until decided.
/// </remarks>
public sealed class CsvExportServiceTests : IDisposable
{
    private const string Header =
        "Typ;Datei;Zeile;Position;Ende;Regelname;SpaCy-Label;Vorherige Zeile;Trefferzeile;Nächste Zeile";

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ps-csv-" + Guid.NewGuid().ToString("N"));

    public CsvExportServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string NewPath(string name = "out.csv") => Path.Combine(_dir, name);

    // The writer stays open during a scan, so the file is read with a shared handle.
    private static string ReadRaw(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string[] Lines(string path) =>
        ReadRaw(path).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

    private static LogEntryViewModel Entry(string hit = "hit", string? prev = "prev", string? next = "next") => new()
    {
        Type = ScanWarningType.Rule,
        FilePath = @"C:\data\a.txt",
        Line = 12,
        Position = 3,
        End = 6,
        RuleName = "Email",
        SuspiciousContent = new SuspiciousContentDto { MatchText = "m", PrevLine = prev, HitLine = hit, NextLine = next },
    };

    [Fact]
    public void BeforeInitialisation_ExportIsInactiveAndWritesAreIgnored()
    {
        using var service = new CsvExportService();

        service.WriteEntry(Entry());

        Assert.False(service.IsExportActive);
    }

    [Fact]
    public void InitializeExport_WritesHeaderImmediatelyAndActivates()
    {
        var path = NewPath();
        using var service = new CsvExportService();

        service.InitializeExport(path);

        Assert.True(service.IsExportActive);
        Assert.Equal(Header + Environment.NewLine, ReadRaw(path));
    }

    [Fact]
    public void WriteEntry_WritesOneSemicolonSeparatedRowAndFlushes()
    {
        var path = NewPath();
        using var service = new CsvExportService();
        service.InitializeExport(path);

        service.WriteEntry(Entry());

        var lines = Lines(path);
        Assert.Equal(2, lines.Length);
        Assert.Equal(@"Rule;C:\data\a.txt;12;3;6;Email;;prev;hit;next", lines[1]);
    }

    [Fact]
    public void WriteEntry_SpacyEntry_WritesLabelAndEmptyRuleName()
    {
        var path = NewPath();
        using var service = new CsvExportService();
        service.InitializeExport(path);
        var entry = Entry(prev: null, next: null);
        entry.Type = ScanWarningType.SpaCy;
        entry.RuleName = null;
        entry.SpacyLabel = SpaCyLabel.Org;

        service.WriteEntry(entry);

        Assert.Equal(@"SpaCy;C:\data\a.txt;12;3;6;;Org;;hit;", Lines(path)[1]);
    }

    [Fact]
    public void WriteEntry_QuotesFieldsWithSeparatorQuoteOrLineBreak()
    {
        var path = NewPath();
        using var service = new CsvExportService();
        service.InitializeExport(path);

        service.WriteEntry(Entry(hit: "a;b", prev: "say \"hi\"", next: "x\r\ny"));
        service.Close();

        var text = ReadRaw(path);
        Assert.Contains("\"a;b\"", text);
        Assert.Contains("\"say \"\"hi\"\"\"", text);
        Assert.Contains("\"x\r\ny\"", text);
    }

    [Fact]
    public void WriteEntry_QuotesFieldWithBareLineFeedOrCarriageReturn()
    {
        var path = NewPath();
        using var service = new CsvExportService();
        service.InitializeExport(path);

        service.WriteEntry(Entry(hit: "a\nb", prev: "c\rd"));
        service.Close();

        var text = ReadRaw(path);
        Assert.Contains("\"a\nb\"", text);
        Assert.Contains("\"c\rd\"", text);
    }

    [Fact]
    public void WriteEntry_UsesUtf8AndKeepsUmlauts()
    {
        var path = NewPath();
        using var service = new CsvExportService();
        service.InitializeExport(path);

        service.WriteEntry(Entry(hit: "Müller Straße"));
        service.Close();

        Assert.Contains("Müller Straße", ReadRaw(path));
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, File.ReadAllBytes(path).Take(3).ToArray());
    }

    [Fact]
    public void Close_DeactivatesAndLaterWritesAreIgnored()
    {
        var path = NewPath();
        using var service = new CsvExportService();
        service.InitializeExport(path);
        service.Close();

        service.WriteEntry(Entry());

        Assert.False(service.IsExportActive);
        Assert.Equal(Header + Environment.NewLine, File.ReadAllText(path));
    }

    [Fact]
    public void Close_CalledTwiceIsHarmless()
    {
        using var service = new CsvExportService();
        service.InitializeExport(NewPath());

        service.Close();
        service.Close();

        Assert.False(service.IsExportActive);
    }

    [Fact]
    public void InitializeExport_Again_ClosesTheOldFileAndStartsTheNew()
    {
        var first = NewPath("first.csv");
        var second = NewPath("second.csv");
        using var service = new CsvExportService();
        service.InitializeExport(first);
        service.WriteEntry(Entry(hit: "one"));

        service.InitializeExport(second);
        service.WriteEntry(Entry(hit: "two"));
        service.Close();

        Assert.Contains("one", File.ReadAllText(first));
        Assert.DoesNotContain("two", File.ReadAllText(first));
        Assert.Contains("two", File.ReadAllText(second));
    }

    [Fact]
    public void InitializeExport_SamePathAgain_OverwritesInsteadOfAppending()
    {
        var path = NewPath();
        using var service = new CsvExportService();
        service.InitializeExport(path);
        service.WriteEntry(Entry(hit: "old"));

        service.InitializeExport(path);
        service.Close();

        Assert.DoesNotContain("old", File.ReadAllText(path));
    }

    [Fact]
    public void Dispose_ReleasesTheFile()
    {
        var path = NewPath();
        var service = new CsvExportService();
        service.InitializeExport(path);

        service.Dispose();

        Assert.False(service.IsExportActive);
        File.Delete(path);
        Assert.False(File.Exists(path));
    }
}
