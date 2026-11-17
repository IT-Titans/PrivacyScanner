using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Helpers;
using ITTitans.PrivacyScanner.Model;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Helpers;

/// <summary>Verifies that <see cref="SpaCyLabelConverter"/> reads and writes the spaCy string labels.</summary>
public class SpaCyLabelConverterTests
{
    private static JsonSerializerOptions Options()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new SpaCyLabelConverter());
        return options;
    }

    [Theory]
    [InlineData(SpaCyLabel.Per, "\"PER\"")]
    [InlineData(SpaCyLabel.Loc, "\"LOC\"")]
    [InlineData(SpaCyLabel.Org, "\"ORG\"")]
    [InlineData(SpaCyLabel.Misc, "\"MISC\"")]
    [InlineData(SpaCyLabel.Unknown, "\"UNKNOWN\"")]
    public void Serialize_WritesUpperCaseLabel(SpaCyLabel label, string expectedJson)
    {
        Assert.Equal(expectedJson, JsonSerializer.Serialize(label, Options()));
    }

    [Theory]
    [InlineData("\"PER\"", SpaCyLabel.Per)]
    [InlineData("\"person\"", SpaCyLabel.Per)]
    [InlineData("\"GPE\"", SpaCyLabel.Loc)]
    [InlineData("\"ORGANIZATION\"", SpaCyLabel.Org)]
    [InlineData("\"MISC\"", SpaCyLabel.Misc)]
    [InlineData("\"SOMETHING_NEW\"", SpaCyLabel.Unknown)]
    [InlineData("null", SpaCyLabel.Unknown)]
    public void Deserialize_MapsSpacyLabelToEnum(string json, SpaCyLabel expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<SpaCyLabel>(json, Options()));
    }

    [Theory]
    [InlineData(SpaCyLabel.Per)]
    [InlineData(SpaCyLabel.Loc)]
    [InlineData(SpaCyLabel.Org)]
    [InlineData(SpaCyLabel.Misc)]
    [InlineData(SpaCyLabel.Unknown)]
    public void Serialize_ThenDeserialize_RoundTripsEveryLabel(SpaCyLabel label)
    {
        var json = JsonSerializer.Serialize(label, Options());

        Assert.Equal(label, JsonSerializer.Deserialize<SpaCyLabel>(json, Options()));
    }
}
