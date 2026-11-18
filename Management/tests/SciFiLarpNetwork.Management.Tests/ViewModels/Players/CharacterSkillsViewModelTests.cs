using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels.Players;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Players;

public sealed class CharacterSkillsViewModelTests
{
    [Fact]
    public async Task WindowShowsTheCharactersTalentsSkillsAndFreePoints()
    {
        var (api, _) = CharacterWithFlying(experiencePoints: 30);

        var skills = await OpenSkillsAsync(api);

        Assert.Equal(
            new { Title = "Talents and skills of Kira", Talents = "Pilot", Skills = "Flying", FreePoints = "Free points: 20" },
            new
            {
                skills.Title,
                Talents = string.Join(',', skills.Talents.Select(talent => talent.Title)),
                Skills = string.Join(',', skills.Skills.Select(skill => skill.Title)),
                FreePoints = skills.FreePointsText
            });
    }

    [Fact]
    public async Task TalentsOnOfferLeaveOutArchivedAndOwnedTalents()
    {
        var (api, _) = CharacterWithFlying(experiencePoints: 30);
        api.AddTalent("Medic");
        api.AddTalent("Smuggler", isArchived: true);

        var skills = await OpenSkillsAsync(api);

        Assert.Equal(["Medic"], skills.AvailableTalents.Select(talent => talent.Title));
    }

    [Fact]
    public async Task SkillsOnOfferAreUnownedUnarchivedSkillsOfTheCharactersTalents()
    {
        var (api, _) = CharacterWithFlying(experiencePoints: 30);
        var pilot = api.Talents[0];
        api.AddSkill("Gunnery", 15, pilot);
        api.AddSkill("Old tricks", 1, pilot, isArchived: true);
        api.AddSkill("First aid", 5, api.AddTalent("Medic"));

        var skills = await OpenSkillsAsync(api);

        Assert.Equal(["Gunnery — Pilot (15 points)"], skills.AvailableSkills.Select(offer => offer.Title));
    }

    [Fact]
    public async Task BuyingASkillLowersTheFreePoints()
    {
        var (api, _) = CharacterWithFlying(experiencePoints: 30);
        api.AddSkill("Gunnery", 15, api.Talents[0]);
        var skills = await OpenSkillsAsync(api);

        skills.AddSkillCommand.Execute(null);

        Assert.Equal(5, skills.FreePoints);
    }

    [Fact]
    public async Task RaisingPurchasesLowersTheFreePoints()
    {
        var api = new FakePlayerApi();
        var kira = api.AddCharacter(api.AddPlayer("Ann"), "Kira");
        kira.ExperiencePoints = 30;
        var medic = api.AddTalent("Medic");
        var stamina = api.AddSkill("Stamina", 5, medic, isMultiPurchase: true);
        kira.TalentIds = [medic.Id];
        kira.Purchases = [new FakePurchase(stamina.Id, medic.Id, 1, null)];
        var skills = await OpenSkillsAsync(api);

        skills.Skills[0].Count = "4";

        Assert.Equal(10, skills.FreePoints);
    }

    [Fact]
    public async Task SkillsBoughtOnlyOnceCannotChangeTheirPurchases()
    {
        var (api, _) = CharacterWithFlying(experiencePoints: 30);

        var skills = await OpenSkillsAsync(api);

        Assert.False(skills.Skills[0].CanChangeCount);
    }

    [Fact]
    public async Task NegativeFreePointsShowTheWarning()
    {
        var (api, _) = CharacterWithFlying(experiencePoints: 5);

        var skills = await OpenSkillsAsync(api);

        Assert.True(skills.HasNegativeFreePoints);
    }

    [Fact]
    public async Task TalentCannotBeAddedBeyondTheLimit()
    {
        var api = new FakePlayerApi();
        var kira = api.AddCharacter(api.AddPlayer("Ann"), "Kira");
        kira.TalentIds = [.. Enumerable.Range(1, 5).Select(number => api.AddTalent($"Talent {number}").Id)];
        api.AddTalent("One more");

        var skills = await OpenSkillsAsync(api);

        Assert.False(skills.AddTalentCommand.CanExecute(null));
    }

    [Fact]
    public async Task TalentLimitFollowsTheServer()
    {
        var api = new FakePlayerApi { TalentLimit = 2 };
        var kira = api.AddCharacter(api.AddPlayer("Ann"), "Kira");
        kira.TalentIds = [api.AddTalent("Pilot").Id, api.AddTalent("Medic").Id];
        api.AddTalent("Smuggler");

        var skills = await OpenSkillsAsync(api);

        Assert.Equal(
            new { CanAdd = false, Limit = "A character can have up to 2 talents." },
            new { CanAdd = skills.AddTalentCommand.CanExecute(null), Limit = skills.TalentLimitText });
    }

    [Fact]
    public async Task TalentWithASkillBoughtThroughItCannotBeRemoved()
    {
        var (api, _) = CharacterWithFlying(experiencePoints: 30);

        var skills = await OpenSkillsAsync(api);

        Assert.False(skills.Talents[0].RemoveCommand.CanExecute(null));
    }

    [Fact]
    public async Task RemovingTheSkillLetsItsTalentBeRemoved()
    {
        var (api, _) = CharacterWithFlying(experiencePoints: 30);
        var skills = await OpenSkillsAsync(api);

        skills.Skills[0].RemoveCommand.Execute(null);

        Assert.True(skills.Talents[0].RemoveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveSendsBothListsWithTheCharacterVersion()
    {
        var (api, _) = CharacterWithFlying(experiencePoints: 30);
        var pilot = api.Talents[0];
        var flying = api.Skills[0];
        var medic = api.AddTalent("Medic");
        var skills = await OpenSkillsAsync(api);
        skills.SelectedNewTalent = skills.AvailableTalents.Single(talent => talent.TalentId == medic.Id);
        skills.AddTalentCommand.Execute(null);
        skills.Skills[0].Specifics = "Shuttles";

        await skills.SaveCommand.ExecuteAsync();

        Assert.Equal(
            new Submission("save-character-skills",
                $$"""{"version":1,"talentIds":["{{pilot.Id}}","{{medic.Id}}"],"skills":[{"skillId":"{{flying.Id}}","talentId":"{{pilot.Id}}","count":1,"specifics":"Shuttles"}]}"""),
            api.Submissions.Single());
    }

    [Fact]
    public async Task InvalidPurchasesShowTheErrorAndSendNothing()
    {
        var (api, _) = CharacterWithFlying(experiencePoints: 30);
        var skills = await OpenSkillsAsync(api);
        skills.Skills[0].Count = "0";

        await skills.SaveCommand.ExecuteAsync();

        Assert.Equal(
            new { Sent = 0, Error = (string?)"Enter purchases as a whole number from 1 to 999." },
            new { Sent = api.Submissions.Count, Error = skills.StatusMessage });
    }

    [Fact]
    public async Task SaveErrorAboutSkillsNamesThem()
    {
        var (api, _) = CharacterWithFlying(experiencePoints: 30);
        api.SubmitFailures["save-character-skills"] = new ManagementApiException("errors.requiredByOtherSkills", HttpStatusCode.Conflict,
            new Dictionary<string, IReadOnlyList<string>> { ["skills"] = ["Aces", "Stunts"] });
        var skills = await OpenSkillsAsync(api);

        await skills.SaveCommand.ExecuteAsync();

        Assert.Equal(
            new
            {
                Error = (string?)new JsonLocalizer().TranslateApiError("errors.requiredByOtherSkills", "en"),
                Skills = (string?)"Skills: Aces, Stunts"
            },
            new { Error = skills.StatusMessage, Skills = skills.AffectedSkills });
    }

    [Fact]
    public async Task NewCharacterDialogCannotOpenTalentsAndSkills()
    {
        var api = new FakePlayerApi();
        api.AddPlayer("Ann");
        var dialogs = new FakePlayerDialogs();
        var roster = await LoadedRosterAsync(api, dialogs);
        roster.SelectedPlayer = roster.Players[0];

        await roster.NewCharacterCommand.ExecuteAsync();

        Assert.False(dialogs.LastCharacterEditor!.CanManageSkills);
    }

    [Fact]
    public async Task CharacterEditAfterSavingSkillsUsesTheNewVersion()
    {
        var (api, _) = CharacterWithFlying(experiencePoints: 30);
        var dialogs = new FakePlayerDialogs
        {
            OnCharacterEditor = async editor =>
            {
                await editor.ManageSkillsCommand.ExecuteAsync();
                editor.Name = "Kira Nerys";
                await editor.SaveCommand.ExecuteAsync();
            },
            OnCharacterSkills = skills => skills.SaveCommand.ExecuteAsync()
        };
        var roster = await LoadedRosterAsync(api, dialogs);
        roster.SelectedPlayer = roster.Players[0];
        roster.SelectedCharacter = roster.Characters[0];

        await roster.EditCharacterCommand.ExecuteAsync();

        var update = JsonDocument.Parse(api.Submissions.Single(submission => submission.ActionName == "update-character").Body!).RootElement;
        Assert.Equal(2L, update.GetProperty("version").GetInt64());
    }

    private static (FakePlayerApi Api, FakeCharacter Kira) CharacterWithFlying(int experiencePoints)
    {
        var api = new FakePlayerApi();
        var kira = api.AddCharacter(api.AddPlayer("Ann"), "Kira");
        kira.ExperiencePoints = experiencePoints;
        var pilot = api.AddTalent("Pilot");
        var flying = api.AddSkill("Flying", 10, pilot);
        kira.TalentIds = [pilot.Id];
        kira.Purchases = [new FakePurchase(flying.Id, pilot.Id, 1, null)];
        return (api, kira);
    }

    /// <summary>Opens the talents and skills window from the first character's dialog, the way an administrator does.</summary>
    private static async Task<CharacterSkillsViewModel> OpenSkillsAsync(FakePlayerApi api)
    {
        var dialogs = new FakePlayerDialogs { OnCharacterEditor = editor => editor.ManageSkillsCommand.ExecuteAsync() };
        var roster = await LoadedRosterAsync(api, dialogs);
        roster.SelectedPlayer = roster.Players[0];
        roster.SelectedCharacter = roster.Characters[0];
        await roster.EditCharacterCommand.ExecuteAsync();
        return dialogs.LastCharacterSkills!;
    }

    private static async Task<PlayerRosterViewModel> LoadedRosterAsync(FakePlayerApi api, FakePlayerDialogs dialogs)
    {
        var roster = new PlayerRosterViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", "en", FakePlayerApi.PlayersHref);
        await roster.LoadAsync();
        return roster;
    }
}
