using SciFiLarpNetwork.Backend.Application.CharacterCards;

namespace SciFiLarpNetwork.Backend.Application.CardTemplates;

/// <summary>
/// Made-up cards that check and preview a template: an archived character, one with an advanced skill, one with a
/// very long name, and one with no skills, so a template meets the cases real data can bring.
/// </summary>
public static class SampleCharacterCards
{
    public static IReadOnlyList<CharacterCard> Cards { get; } =
    [
        new(1, "Alex Sample", 1, "Nova Ardent", "Captain", "Female", "Human", "Pilot",
            "0f8fad5bd9cb469fa16570867728950e", 120, 15, IsArchived: false,
            [new("Shield", 2), new("Rank", 0)],
            [new("Hit points", "12"), new("Call sign", "Comet")],
            [
                new("Engineering", [new("Repair", 2, IsAdvanced: false), new("Overdrive", 1, IsAdvanced: true)]),
                new("Piloting", [new("Evasive flying", 1, IsAdvanced: false)])
            ]),
        new(2, "Sam Example", 2, "Orrin Vale", null, null, "Synthetic", null,
            "7c9e6679742540de944be07fc1f90ae7", 40, 0, IsArchived: true,
            [new("Shield", 0), new("Rank", 1)],
            [new("Hit points", "8")],
            [new("Medicine", [new("First aid", 1, IsAdvanced: false)])]),
        new(3, "Maria Antonietta Lunghissimo-Cognome Esempio", 3,
            "Seraphina Constellation Vantablack-Oberon of the Ninth Outer Rim Assembly", "High Envoy Plenipotentiary",
            "Non-binary", "Uplifted Cephalopod", "Diplomat and Negotiator",
            "16fd2706d8a54c3a8f8a9d2d1f2e0d6b", 999, -3, IsArchived: false,
            [new("Shield", 9), new("Rank", 5)],
            [new("Hit points", "30"), new("Languages", "Common, Trade, Old High Signal, Deep Choir")],
            [
                new("Diplomacy", [
                    new("Persuasion", 5, IsAdvanced: false),
                    new("Galactic law", 1, IsAdvanced: true),
                    new("Cultural etiquette of the outer colonies", 3, IsAdvanced: false)
                ]),
                new("Science", [new("Xenobiology", 2, IsAdvanced: true), new("Astronomy", 1, IsAdvanced: false)])
            ]),
        new(4, "Kai Placeholder", 4, "Juno", null, null, null, null,
            "9b2f1c3e5d7a4b6c8e0f1a2b3c4d5e6f", 0, 0, IsArchived: false,
            [new("Shield", 0), new("Rank", 0)],
            [],
            [new("Survival", [])])
    ];
}
