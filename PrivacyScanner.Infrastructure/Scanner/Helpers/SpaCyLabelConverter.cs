using System.Text.Json;
using System.Text.Json.Serialization;
using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Helpers;

/// <summary>
/// Serializes and deserializes <see cref="SpaCyLabel"/> values as their spaCy string labels.
/// </summary>
public class SpaCyLabelConverter : JsonConverter<SpaCyLabel>
{
    public override SpaCyLabel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var labelString = reader.GetString();

        return SpaCyLabelMapper.MapToEnum(labelString);
    }

    public override void Write(Utf8JsonWriter writer, SpaCyLabel value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToUpperInvariant());
    }
}
