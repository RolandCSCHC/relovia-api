using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Relovia.Api.Common.OpenApi;

public static class StringEnumSchemaTransformer
{
    public static Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (!type.IsEnum)
        {
            return Task.CompletedTask;
        }

        var nullable = schema.Type?.HasFlag(JsonSchemaType.Null) == true;
        schema.Type = nullable ? JsonSchemaType.String | JsonSchemaType.Null : JsonSchemaType.String;
        schema.Format = null;
        schema.Enum ??= [];
        schema.Enum.Clear();
        foreach (var name in Enum.GetNames(type))
        {
            schema.Enum.Add(JsonValue.Create(name)!);
        }

        return Task.CompletedTask;
    }
}
