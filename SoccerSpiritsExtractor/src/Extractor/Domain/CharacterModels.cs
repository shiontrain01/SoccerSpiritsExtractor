namespace Extractor.Domain;

public sealed record CharacterProfile(
    string Name,
    string Attribute, // Ardor/Dark/Light/Thunder/Whirlwind
    string Rarity,    // e.g. "L"
    string Type,      // Striker/...
    string? Team,
    Dictionary<string, string?> Meta,        // Gender, Spirit Stones etc (opcional)
    Dictionary<string, decimal?> Stats,      // Power/Technique/Vitality/Speed/Dribble/...
    List<CharacterSkill> Skills,             // Active/Passive/Ace/Final etc
    string SourceUrl
);

public sealed record CharacterSkill(
    string Name,
    string Kind,        // Ace Skill / Active / Passive / Final Technique / etc
    string Description
);

public sealed record ExtractionRun(
    string RunId,
    string SourceUrl,
    bool Success,
    string OutputJson,
    string? Error,
    DateTimeOffset CreatedAt
);
