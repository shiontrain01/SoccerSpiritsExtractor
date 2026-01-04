namespace Extractor.Application;

public static class JsonSchema
{
    // Schema forte, mas flexível o suficiente pra wiki
    public const string SoccerSpiritsCharacterSchema = """
{
  "type": "object",
  "properties": {
    "name": { "type": "string" },
    "attribute": { "type": "string" },
    "rarity": { "type": "string" },
    "type": { "type": "string" },
    "team": { "type": ["string","null"] },
    "meta": {
      "type": "object",
      "additionalProperties": { "type": ["string","null"] }
    },
    "stats": {
      "type": "object",
      "additionalProperties": { "type": ["number","null"] }
    },
    "skills": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "name": { "type": "string" },
          "kind": { "type": "string" },
          "description": { "type": "string" }
        },
        "required": ["name","kind","description"],
        "additionalProperties": false
      }
    },
    "sourceUrl": { "type": "string" }
  },
  "required": ["name","attribute","rarity","type","stats","skills","sourceUrl"],
  "additionalProperties": false
}
""";
}
