using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players;

internal static class RosterMappings
{
    extension(PlayerDocument player)
    {
        public PlayerSummary ToSummary() =>
            new(
                player.Id,
                player.Number,
                player.DisplayName,
                player.Email,
                player.Phone,
                player.Notes,
                player.Type,
                player.Status,
                player.IsArchived,
                player.Version);
    }

    extension(CharacterStatDocument stat)
    {
        public CharacterStatSummary ToSummary() =>
            new(stat.Id, stat.Name, stat.Type, stat.DefaultNumber, stat.DefaultText, stat.IsArchived, stat.Version);

        /// <summary>The value of a character that has no value of its own for this stat.</summary>
        public CharacterStatValue DefaultValue() =>
            new(stat.Id, stat.Type == CharacterStatType.Number ? stat.DefaultNumber ?? 0 : null, stat.DefaultText);
    }

    extension(CharacterDocument character)
    {
        /// <param name="stats">Every stat, in the order the summary lists them.</param>
        public CharacterSummary ToSummary(string playerDisplayName, IReadOnlyList<CharacterStatDocument> stats)
        {
            var values = character.StatValues.ToDictionary(value => value.StatId);
            return new CharacterSummary(
                character.Id,
                character.Number,
                character.PlayerId,
                playerDisplayName,
                character.Name,
                character.Title,
                character.Species,
                character.Gender,
                character.Type,
                character.ExperiencePoints,
                character.Status,
                character.DeathDate,
                character.NetworkId,
                character.QrIdentifier,
                stats.Select(stat => ToValueSummary(stat, values.GetValueOrDefault(stat.Id) ?? stat.DefaultValue())).ToArray(),
                character.IsArchived,
                character.Version);
        }
    }

    private static CharacterStatValueSummary ToValueSummary(CharacterStatDocument stat, CharacterStatValue value) =>
        stat.Type == CharacterStatType.Number
            ? new CharacterStatValueSummary(stat.Id, stat.Name, stat.Type, value.Number ?? 0, null, stat.IsArchived)
            : new CharacterStatValueSummary(stat.Id, stat.Name, stat.Type, null, value.Text ?? string.Empty, stat.IsArchived);
}
