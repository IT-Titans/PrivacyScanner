using ITTitans.PrivacyScanner.Infrastructure.Scanner.Helpers;
using ITTitans.PrivacyScanner.Model;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Helpers;

/// <summary>Verifies raw spaCy label strings map to the correct coarse <see cref="SpaCyLabel"/>.</summary>
public class SpaCyLabelMapperTests
{
    [Theory]
    [InlineData("PER", SpaCyLabel.Per)]
    [InlineData("PERSON", SpaCyLabel.Per)]
    [InlineData("per", SpaCyLabel.Per)]
    [InlineData("LOC", SpaCyLabel.Loc)]
    [InlineData("LOCATION", SpaCyLabel.Loc)]
    [InlineData("GPE", SpaCyLabel.Loc)]
    [InlineData("ORG", SpaCyLabel.Org)]
    [InlineData("ORGANIZATION", SpaCyLabel.Org)]
    [InlineData("MISC", SpaCyLabel.Misc)]
    [InlineData("SOMETHING_UNKNOWN", SpaCyLabel.Unknown)]
    [InlineData(null, SpaCyLabel.Unknown)]
    [InlineData("", SpaCyLabel.Unknown)]
    public void MapToEnum_ReturnsExpectedLabel(string? input, SpaCyLabel expected)
    {
        Assert.Equal(expected, SpaCyLabelMapper.MapToEnum(input));
    }
}
