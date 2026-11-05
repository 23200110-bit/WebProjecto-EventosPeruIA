using System.Text.Json;
namespace EventosPeruIA.CORE.Infrastructure.Shared;
internal static class EsquemaChat {
    internal static JsonElement Esquema(bool filtros) => JsonSerializer.Deserialize<JsonElement>(filtros ? """
        {"type":"object","properties":{
          "ubicacion":{"type":["string","null"]},
          "categoria":{"type":["string","null"]},
          "desde":{"anyOf":[{"type":"string","pattern":"^[0-9]{4}-[0-9]{2}-[0-9]{2}$"},{"type":"null"}]},
          "hasta":{"anyOf":[{"type":"string","pattern":"^[0-9]{4}-[0-9]{2}-[0-9]{2}$"},{"type":"null"}]},
          "precioMaximo":{"type":["number","null"],"minimum":0}},
          "required":["ubicacion","categoria","desde","hasta","precioMaximo"],"additionalProperties":false}
        """ : """
        {"type":"object","properties":{
          "respuesta":{"type":"string"},
          "eventosIds":{"type":"array","items":{"type":"integer"}}},
          "required":["respuesta","eventosIds"],"additionalProperties":false}
        """);
}
