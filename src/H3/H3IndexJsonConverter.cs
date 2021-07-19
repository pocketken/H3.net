using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace H3 {

    /// <summary>
    /// Handles (de)serialization of H3Index values to/from JSON using
    /// System.Text.Json.
    /// </summary>
    public class H3IndexJsonConverter : JsonConverter<H3Index> {

        /// <inheritdoc/>
        public override H3Index Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Expected string");

            return new H3Index(reader.GetString());
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, H3Index value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString());
        }
    }

}
