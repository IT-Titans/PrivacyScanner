using System.Text.Json;
using System.Text.Json.Serialization;
using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Helpers;

public class SpaCyLabelConverter : JsonConverter<SpaCyLabel>
{
    public override SpaCyLabel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var labelString = reader.GetString();

        return labelString?.ToUpperInvariant() switch
        {
            "PER" or "PERSON" => SpaCyLabel.Per,
            "LOC" or "LOCATION" or "GPE" => SpaCyLabel.Loc,
            "ORG" or "ORGANIZATION" => SpaCyLabel.Org,
            "MISC" => SpaCyLabel.Misc,
            _ => SpaCyLabel.Unknown
        };
    }

    public override void Write(Utf8JsonWriter writer, SpaCyLabel value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToUpperInvariant());
    }
}

public static class SpaCyLabelMapper
{
    public static SpaCyLabel MapToEnum(string? label)
    {
        return label?.ToUpperInvariant() switch
        {
            "PER" or "PERSON" => SpaCyLabel.Per,
            "LOC" or "LOCATION" or "GPE" => SpaCyLabel.Loc,
            "ORG" or "ORGANIZATION" => SpaCyLabel.Org,
            "MISC" => SpaCyLabel.Misc,
            _ => SpaCyLabel.Unknown
        };
    }
}
