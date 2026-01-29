using CrewService.BlazorUI.Models.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrewService.BlazorUI.Converters;

public class ParentListConverter : JsonConverter<ParentList>
{
    public override ParentList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var parentList = new ParentList();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "parent")
            {
                reader.Read();
                parentList.Parents = JsonSerializer.Deserialize<List<Parent>>(ref reader, options) ?? [];
            }
        }

        return parentList;
    }

    public override void Write(Utf8JsonWriter writer, ParentList value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("parent");
        JsonSerializer.Serialize(writer, value.Parents, options);
        writer.WriteEndObject();
    }
}

