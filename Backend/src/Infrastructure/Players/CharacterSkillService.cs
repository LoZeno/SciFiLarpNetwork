using Marten;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.CharacterSkills;
using SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players;

/// <summary>
/// Saving follows Marten's command handler workflow: <c>FetchForWriting</c> on the character stream at the edited
/// version, then one <c>SaveChangesAsync</c>. Talent and skill streams are only read. A catalog change committed at the
/// same moment (a skill archived, its cost or prerequisites changed, or it is taken off a talent) leaves the
/// character in a state the specification allows for catalog changes made after a purchase, so those streams are not
/// locked. Experience points live on the character stream, so lowering them conflicts with this command.
/// </summary>
internal sealed class CharacterSkillService(AttributedSessionFactory sessions, TimeProvider timeProvider)
    : ICharacterSkillService
{
    public async Task<CharacterSkillsSummary?> GetCharacterSkillsAsync(Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        return await session.LoadAsync<CharacterDocument>(characterId, cancellationToken) is { } character
            ? ToSummary(character, await LoadCatalogAsync(session, character.TalentIds, character.SkillPurchases, cancellationToken))
            : null;
    }

    public async Task<CharacterSkillsResult> SaveCharacterSkillsAsync(
        Guid characterId,
        long expectedVersion,
        CharacterSkillsDefinition definition,
        CancellationToken cancellationToken = default)
    {
        if (CharacterSkillsDefinitionValidator.Validate(definition) is { } invalid)
        {
            return Failure(invalid);
        }

        await using var session = sessions.OpenSession();
        var (fetchOutcome, stream) =
            await session.FetchExpectedAsync<CharacterDocument>(characterId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return Failure(fetchOutcome);
        }

        var talentIds = definition.TalentIds.Distinct().ToArray();
        var purchases = definition.Purchases
            .Select(purchase => purchase with { Specifics = CharacterTexts.Normalize(purchase.Specifics) })
            .ToArray();
        var catalog = await LoadCatalogAsync(
            session,
            [.. talentIds, .. current.TalentIds],
            [.. purchases, .. current.SkillPurchases],
            cancellationToken);
        var finding = CharacterSkillRules.Check(new CharacterSkillChange(
            current.ExperiencePoints,
            current.TalentIds,
            current.SkillPurchases,
            talentIds,
            purchases,
            catalog.Talents.Values.ToDictionary(talent => talent.Id, talent => new TalentFacts(talent.Id, talent.IsArchived)),
            catalog.Skills.Values.ToDictionary(skill => skill.Id, ToFacts)));
        if (finding.Problem != CharacterSkillProblem.None)
        {
            return Failure(ToOutcome(finding.Problem), SkillNames(finding.SkillIds, catalog));
        }

        if (talentIds.SequenceEqual(current.TalentIds) && purchases.SequenceEqual(current.SkillPurchases))
        {
            return Success(ToSummary(current, catalog));
        }

        stream.AppendOne(new CharacterSkillsChanged(characterId, talentIds, purchases, timeProvider.GetUtcNow()));
        // Only the character stream is appended to, so a rejected commit means the character itself moved on.
        var outcome = await session.CommitAsync(
            () => Task.FromResult(RosterOutcome.StaleVersion),
            RosterWrites.ConcurrentChange,
            cancellationToken);
        if (outcome != RosterOutcome.Succeeded)
        {
            return Failure(outcome);
        }

        var saved = await session.LoadAsync<CharacterDocument>(characterId, cancellationToken);
        return Success(ToSummary(saved!, catalog));
    }

    private static CharacterSkillsResult Success(CharacterSkillsSummary summary) =>
        new(RosterOutcome.Succeeded, summary, []);

    private static CharacterSkillsResult Failure(RosterOutcome outcome, IReadOnlyList<string>? skillNames = null) =>
        new(outcome, null, skillNames ?? []);

    private static RosterOutcome ToOutcome(CharacterSkillProblem problem) =>
        problem switch
        {
            CharacterSkillProblem.UnknownTalent => RosterOutcome.UnknownTalent,
            CharacterSkillProblem.TalentArchived => RosterOutcome.TalentArchived,
            CharacterSkillProblem.UnknownSkill => RosterOutcome.UnknownSkill,
            CharacterSkillProblem.TalentNotOwned => RosterOutcome.TalentNotOwned,
            CharacterSkillProblem.SkillNotInTalent => RosterOutcome.SkillNotInTalent,
            CharacterSkillProblem.SkillArchived => RosterOutcome.SkillArchived,
            CharacterSkillProblem.NotMultiPurchase => RosterOutcome.NotMultiPurchase,
            CharacterSkillProblem.PrerequisitesNotMet => RosterOutcome.PrerequisitesNotMet,
            CharacterSkillProblem.RequiredByOtherSkills => RosterOutcome.RequiredByOtherSkills,
            CharacterSkillProblem.InsufficientFreePoints => RosterOutcome.InsufficientFreePoints,
            _ => throw new ArgumentOutOfRangeException(nameof(problem), problem, "No problem is not a failure.")
        };

    /// <summary>Names of the skills a problem is about; a skill that does not exist is named by its identifier.</summary>
    private static string[] SkillNames(IEnumerable<Guid> skillIds, Catalog catalog) =>
        skillIds
            .Select(skillId => catalog.Skills.TryGetValue(skillId, out var skill) ? skill.Name : skillId.ToString("D"))
            .Order(StringComparer.CurrentCulture)
            .ToArray();

    private static SkillFacts ToFacts(SkillDocument skill) =>
        new(skill.Id, skill.Cost, skill.IsMultiPurchase, skill.IsArchived, skill.TalentIds, skill.PrerequisiteGroups);

    private static async Task<Catalog> LoadCatalogAsync(
        IQuerySession session,
        IEnumerable<Guid> talentIds,
        IEnumerable<SkillPurchase> purchases,
        CancellationToken cancellationToken)
    {
        var talentKeys = talentIds.Distinct().ToArray();
        var skillKeys = purchases.Select(purchase => purchase.SkillId).Distinct().ToArray();
        var talents = talentKeys.Length == 0
            ? []
            : await session.LoadManyAsync<TalentDocument>(cancellationToken, talentKeys);
        var skills = skillKeys.Length == 0
            ? []
            : await session.LoadManyAsync<SkillDocument>(cancellationToken, skillKeys);
        return new Catalog(talents.ToDictionary(talent => talent.Id), skills.ToDictionary(skill => skill.Id));
    }

    private static CharacterSkillsSummary ToSummary(CharacterDocument character, Catalog catalog) =>
        new(
            character.Id,
            character.Name,
            character.ExperiencePoints,
            CharacterSkillRules.FreePoints(character.ExperiencePoints, character.SkillPurchases,
                skillId => catalog.Skills[skillId].Cost),
            character.TalentIds
                .Select(talentId => catalog.Talents[talentId])
                .Select(talent => new CharacterTalentSummary(talent.Id, talent.Name, talent.IsArchived))
                .ToArray(),
            character.SkillPurchases
                .Select(purchase => (Purchase: purchase, Skill: catalog.Skills[purchase.SkillId]))
                .OrderBy(owned => owned.Skill.NormalizedName, StringComparer.Ordinal)
                .Select(owned => new CharacterSkillPurchaseSummary(
                    owned.Skill.Id,
                    owned.Skill.Name,
                    owned.Purchase.TalentId,
                    owned.Purchase.Count,
                    owned.Purchase.Specifics,
                    owned.Skill.Cost,
                    owned.Skill.IsMultiPurchase,
                    owned.Skill.IsArchived))
                .ToArray(),
            character.Version);

    private sealed record Catalog(
        IReadOnlyDictionary<Guid, TalentDocument> Talents,
        IReadOnlyDictionary<Guid, SkillDocument> Skills);
}
