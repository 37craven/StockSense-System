using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockSense.Web.Helpers; // Adjust namespace to match your project

public class PhDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var incomingDate = reader.GetDateTime();
        // This strips the offset entirely. "Saturday +08:00" becomes just "Saturday".
        return DateTime.SpecifyKind(incomingDate, DateTimeKind.Unspecified);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // This sends the date back to Blazor WITHOUT a timezone attached, 
        // ensuring Blazor displays exactly what is in the database.
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss"));
    }
}