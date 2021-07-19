using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace H3 {

    /// <summary>
    /// Handles (de)serialization of H3Index values to/from JSON using
    /// System.Text.Json.
    /// </summary>
    public class H3IndexJsonConverter : JsonConverter<H3Index> {
        private static readonly Regex H3Pattern = new("^[a-f0-9]{15}$");

        /// <inheritdoc/>
        public override H3Index Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Expected string");

            var value = reader.GetString();
            if (!H3Pattern.IsMatch(value)) {
                throw new JsonException("Not a valid H3 hex string");
            }

            return new H3Index(value);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, H3Index value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString());
        }
    }

}
