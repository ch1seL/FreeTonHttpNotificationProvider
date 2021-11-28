using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Notifon.Server.Database {
    public static class ValueConversionExtensions {
        private static readonly JsonSerializerOptions NullJsonSerializerOptions = null;
        
        public static void HasJsonConversion<T>(this PropertyBuilder<T> propertyBuilder) where T : class, new() {
            var converter = new ValueConverter<T, string>
            (
                v => JsonSerializer.Serialize(v, NullJsonSerializerOptions),
                v => JsonSerializer.Deserialize<T>(v, NullJsonSerializerOptions) ?? new T()
            );

            var comparer = new ValueComparer<T>
            (
                (l, r) => JsonSerializer.Serialize(l, NullJsonSerializerOptions) == JsonSerializer.Serialize(r, NullJsonSerializerOptions),
                v => v == null ? 0 : JsonSerializer.Serialize(v, NullJsonSerializerOptions).GetHashCode(),
                v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, NullJsonSerializerOptions), NullJsonSerializerOptions)
            );

            propertyBuilder.HasConversion(converter);
            propertyBuilder.Metadata.SetValueConverter(converter);
            propertyBuilder.Metadata.SetValueComparer(comparer);
            propertyBuilder.HasColumnType("jsonb");
        }
    }
}