using System.Net;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Skills;

public sealed class TalentCatalogViewModelTests
{
    [Fact]
    public async Task LoadListsActiveTalents()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine");
        api.AddTalent("Old ways", isArchived: true);
        var catalog = CreateCatalog(api);

        await catalog.LoadAsync();

        Assert.Equal(["Medicine"], catalog.Talents.Select(talent => talent.DisplayName));
    }

    [Fact]
    public async Task ShowingArchivedTalentsMarksThem()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine");
        api.AddTalent("Old ways", isArchived: true);
        var catalog = CreateCatalog(api);
        await catalog.LoadAsync();

        catalog.ShowArchivedTalents = true;

        Assert.Equal(["Medicine", "Old ways (Archived)"], catalog.Talents.Select(talent => talent.DisplayName));
    }

    [Fact]
    public async Task EmptyCatalogShowsTheNoResultsText()
    {
        var catalog = CreateCatalog(new FakeCatalogApi());

        await catalog.LoadAsync();

        Assert.Equal("No results found.", catalog.TalentsPlaceholder);
    }

    [Fact]
    public async Task WithoutASelectedTalentTheSkillsListAsksForOne()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine");
        var catalog = CreateCatalog(api);

        await catalog.LoadAsync();

        Assert.Equal("Select a talent to see its skills.", catalog.SkillsPlaceholder);
    }

    [Fact]
    public async Task SelectingATalentListsOnlyItsSkills()
    {
        var api = new FakeCatalogApi();
        var medicine = api.AddTalent("Medicine");
        var engineering = api.AddTalent("Engineering");
        api.AddSkill("First aid", medicine);
        api.AddSkill("Welding", engineering);
        var catalog = CreateCatalog(api);
        await catalog.LoadAsync();

        catalog.SelectedTalent = catalog.Talents.Single(talent => talent.Name == "Medicine");

        Assert.Equal(["First aid"], catalog.Skills.Select(skill => skill.DisplayName));
    }

    [Fact]
    public async Task ShowingArchivedSkillsMarksThem()
    {
        var api = new FakeCatalogApi();
        var medicine = api.AddTalent("Medicine");
        api.AddSkill("Leeches", medicine).IsArchived = true;
        var catalog = CreateCatalog(api);
        await catalog.LoadAsync();
        catalog.SelectedTalent = catalog.Talents[0];

        catalog.ShowArchivedSkills = true;

        Assert.Equal(["Leeches (Archived)"], catalog.Skills.Select(skill => skill.DisplayName));
    }

    [Fact]
    public async Task ChangingTheTalentClearsTheSelectedSkill()
    {
        var api = new FakeCatalogApi();
        var medicine = api.AddTalent("Medicine");
        api.AddTalent("Engineering");
        api.AddSkill("First aid", medicine);
        var catalog = CreateCatalog(api);
        await catalog.LoadAsync();
        catalog.SelectedTalent = catalog.Talents[0];
        catalog.SelectedSkill = catalog.Skills[0];

        catalog.SelectedTalent = catalog.Talents[1];

        Assert.Null(catalog.SelectedSkill);
    }

    [Fact]
    public async Task ReadersCannotChangeTheCatalog()
    {
        var api = new FakeCatalogApi { CanWrite = false };
        api.AddTalent("Medicine");
        var catalog = CreateCatalog(api);

        await catalog.LoadAsync();

        Assert.False(catalog.CanWrite);
    }

    [Fact]
    public async Task AdministratorsCanChangeTheCatalog()
    {
        var catalog = CreateCatalog(new FakeCatalogApi());

        await catalog.LoadAsync();

        Assert.True(catalog.CanWrite);
    }

    [Fact]
    public async Task NewTalentIsAddedToTheList()
    {
        var api = new FakeCatalogApi();
        var dialogs = new FakeCatalogDialogs { OnTalentEditor = editor => SaveAs(editor, "Medicine") };
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();

        await catalog.NewTalentCommand.ExecuteAsync();

        Assert.Equal(["Medicine"], catalog.Talents.Select(talent => talent.DisplayName));
    }

    [Fact]
    public async Task NewTalentSendsItsNameAndDescription()
    {
        var api = new FakeCatalogApi();
        var dialogs = new FakeCatalogDialogs
        {
            OnTalentEditor = editor =>
            {
                editor.Description = "Healing arts";
                return SaveAs(editor, "Medicine");
            }
        };
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();

        await catalog.NewTalentCommand.ExecuteAsync();

        Assert.Equal(
            new Submission("create-talent", """{"name":"Medicine","description":"Healing arts"}"""),
            api.Submissions.Single());
    }

    [Fact]
    public async Task EditTalentStartsFromTheSavedName()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine");
        var dialogs = new FakeCatalogDialogs();
        var catalog = await LoadWithSelectedTalentAsync(api, dialogs);

        await catalog.EditTalentCommand.ExecuteAsync();

        Assert.Equal("Medicine", dialogs.LastTalentEditor?.Name);
    }

    [Fact]
    public async Task EditTalentSendsTheVersionItStartedFrom()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine").Version = 4;
        var catalog = await LoadWithSelectedTalentAsync(api, new FakeCatalogDialogs { OnTalentEditor = editor => SaveAs(editor, "Surgery") });

        await catalog.EditTalentCommand.ExecuteAsync();

        Assert.Equal(new Submission("update-talent", """{"version":4,"name":"Surgery"}"""), api.Submissions.Single());
    }

    [Fact]
    public async Task EditedTalentKeepsItsSelection()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine");
        var catalog = await LoadWithSelectedTalentAsync(api, new FakeCatalogDialogs { OnTalentEditor = editor => SaveAs(editor, "Surgery") });

        await catalog.EditTalentCommand.ExecuteAsync();

        Assert.Equal("Surgery", catalog.SelectedTalent?.Name);
    }

    [Fact]
    public async Task StaleTalentEditShowsTheConflictInTheDialog()
    {
        var api = new FakeCatalogApi();
        var talent = api.AddTalent("Medicine");
        var dialogs = new FakeCatalogDialogs
        {
            OnTalentEditor = editor =>
            {
                talent.Version = 2;
                return SaveAs(editor, "Surgery");
            }
        };
        var catalog = await LoadWithSelectedTalentAsync(api, dialogs);

        await catalog.EditTalentCommand.ExecuteAsync();

        Assert.Equal("Someone else changed this item. Reload it and try again.", dialogs.LastTalentEditor?.ErrorMessage);
    }

    [Fact]
    public async Task StaleTalentEditReloadsTheLatestValues()
    {
        var api = new FakeCatalogApi();
        var talent = api.AddTalent("Medicine");
        var dialogs = new FakeCatalogDialogs
        {
            OnTalentEditor = editor =>
            {
                talent.Name = "Field medicine";
                talent.Version = 2;
                return SaveAs(editor, "Surgery");
            }
        };
        var catalog = await LoadWithSelectedTalentAsync(api, dialogs);

        await catalog.EditTalentCommand.ExecuteAsync();

        Assert.Equal("Field medicine", dialogs.LastTalentEditor?.Name);
    }

    [Fact]
    public async Task SavingAgainAfterAConflictUsesTheReloadedVersion()
    {
        var api = new FakeCatalogApi();
        var talent = api.AddTalent("Medicine");
        var dialogs = new FakeCatalogDialogs
        {
            OnTalentEditor = async editor =>
            {
                talent.Version = 2;
                await SaveAs(editor, "Surgery");
                await SaveAs(editor, "Surgery");
            }
        };
        var catalog = await LoadWithSelectedTalentAsync(api, dialogs);

        await catalog.EditTalentCommand.ExecuteAsync();

        Assert.Equal("Surgery", talent.Name);
    }

    [Fact]
    public async Task RejectedTalentKeepsTheDialogOpen()
    {
        var api = new FakeCatalogApi();
        api.SubmitFailures["create-talent"] = new ManagementApiException("errors.nameAlreadyUsed", HttpStatusCode.Conflict);
        var dialogs = new FakeCatalogDialogs { OnTalentEditor = editor => SaveAs(editor, "Medicine") };
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();

        await catalog.NewTalentCommand.ExecuteAsync();

        Assert.Equal(
            new { IsSaved = false, Error = (string?)"This name is already used." },
            new { dialogs.LastTalentEditor!.IsSaved, Error = dialogs.LastTalentEditor.ErrorMessage });
    }

    [Fact]
    public async Task CancelledTalentDialogSendsNothing()
    {
        var api = new FakeCatalogApi();
        var catalog = CreateCatalog(api, new FakeCatalogDialogs());
        await catalog.LoadAsync();

        await catalog.NewTalentCommand.ExecuteAsync();

        Assert.Empty(api.Submissions);
    }

    [Fact]
    public async Task ArchivingATalentAsksForConfirmationByName()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine");
        var dialogs = new FakeCatalogDialogs { ConfirmResult = false };
        var catalog = await LoadWithSelectedTalentAsync(api, dialogs);

        await catalog.ToggleTalentArchiveCommand.ExecuteAsync();

        Assert.Equal(
            new ConfirmationRequest(
                "Please confirm",
                "Archive the talent \"Medicine\"? Its skills keep their assignment.",
                "Archive",
                "Cancel"),
            dialogs.LastConfirmation);
    }

    [Fact]
    public async Task DeclinedArchiveSendsNothing()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine");
        var catalog = await LoadWithSelectedTalentAsync(api, new FakeCatalogDialogs { ConfirmResult = false });

        await catalog.ToggleTalentArchiveCommand.ExecuteAsync();

        Assert.Empty(api.Submissions);
    }

    [Fact]
    public async Task ConfirmedArchiveHidesTheTalentAndClearsTheSelection()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine");
        var catalog = await LoadWithSelectedTalentAsync(api, new FakeCatalogDialogs());

        await catalog.ToggleTalentArchiveCommand.ExecuteAsync();

        Assert.Equal(
            new { Count = 0, Selected = (TalentListItem?)null },
            new { catalog.Talents.Count, Selected = catalog.SelectedTalent });
    }

    [Fact]
    public async Task ArchivedTalentOffersUnarchive()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Old ways", isArchived: true);
        var catalog = CreateCatalog(api);
        await catalog.LoadAsync();
        catalog.ShowArchivedTalents = true;

        catalog.SelectedTalent = catalog.Talents[0];

        Assert.Equal("Unarchive", catalog.TalentArchiveLabel);
    }

    [Fact]
    public async Task UnarchivingASkillRestoresIt()
    {
        var api = new FakeCatalogApi();
        var medicine = api.AddTalent("Medicine");
        var leeches = api.AddSkill("Leeches", medicine);
        leeches.IsArchived = true;
        var catalog = await LoadWithSelectedTalentAsync(api, new FakeCatalogDialogs());
        catalog.ShowArchivedSkills = true;
        catalog.SelectedSkill = catalog.Skills[0];

        await catalog.ToggleSkillArchiveCommand.ExecuteAsync();

        Assert.False(leeches.IsArchived);
    }

    [Fact]
    public async Task NewSkillIsAssignedToTheSelectedTalentByDefault()
    {
        var api = new FakeCatalogApi();
        var medicine = api.AddTalent("Medicine");
        api.AddTalent("Engineering");
        var dialogs = new FakeCatalogDialogs();
        var catalog = await LoadWithSelectedTalentAsync(api, dialogs);

        await catalog.NewSkillCommand.ExecuteAsync();

        Assert.Equal([medicine.Id.ToString("D")], SelectedValues(dialogs.LastSkillEditor!.Talents));
    }

    [Fact]
    public async Task NewSkillOffersEveryGameFlag()
    {
        var dialogs = new FakeCatalogDialogs();
        var catalog = CreateCatalog(new FakeCatalogApi(), dialogs);
        await catalog.LoadAsync();

        await catalog.NewSkillCommand.ExecuteAsync();

        Assert.Equal(["Access Hacking", "Improves Hacking"], dialogs.LastSkillEditor!.GameFlags.Select(flag => flag.Title));
    }

    [Fact]
    public async Task NewSkillOffersExistingCardFlags()
    {
        var api = new FakeCatalogApi();
        api.CardFlags.Add(new FakeCardFlag(Guid.NewGuid(), "Toughness"));
        var dialogs = new FakeCatalogDialogs();
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();

        await catalog.NewSkillCommand.ExecuteAsync();

        Assert.Equal(["Toughness"], dialogs.LastSkillEditor!.CardFlags.Select(flag => flag.Title));
    }

    [Fact]
    public async Task SkillDialogHidesArchivedTalentsTheSkillDoesNotUse()
    {
        var api = new FakeCatalogApi();
        var medicine = api.AddTalent("Medicine");
        var oldWays = api.AddTalent("Old ways", isArchived: true);
        api.AddTalent("Forgotten", isArchived: true);
        api.AddSkill("Leeches", medicine, oldWays);
        var dialogs = new FakeCatalogDialogs();
        var catalog = await LoadWithSelectedSkillAsync(api, dialogs);

        await catalog.EditSkillCommand.ExecuteAsync();

        Assert.Equal(["Medicine", "Old ways (Archived)"], dialogs.LastSkillEditor!.Talents.Select(talent => talent.Title));
    }

    [Fact]
    public async Task SkillDialogDoesNotOfferTheSkillAsItsOwnPrerequisite()
    {
        var api = new FakeCatalogApi();
        var medicine = api.AddTalent("Medicine");
        api.AddSkill("First aid", medicine);
        api.AddSkill("Surgery");
        var dialogs = new FakeCatalogDialogs();
        var catalog = await LoadWithSelectedSkillAsync(api, dialogs);

        await catalog.EditSkillCommand.ExecuteAsync();

        Assert.Equal(["Surgery"], dialogs.LastSkillEditor!.PrerequisiteCandidates.Select(skill => skill.Name));
    }

    [Fact]
    public async Task EditSkillStartsFromTheSavedPrerequisiteGroups()
    {
        var api = new FakeCatalogApi();
        var medicine = api.AddTalent("Medicine");
        var anatomy = api.AddSkill("Anatomy");
        var biology = api.AddSkill("Biology");
        var chemistry = api.AddSkill("Chemistry");
        api.AddSkill("Surgery", medicine).PrerequisiteGroups.AddRange([[anatomy.Id], [biology.Id, chemistry.Id]]);
        var dialogs = new FakeCatalogDialogs();
        var catalog = await LoadWithSelectedSkillAsync(api, dialogs);

        await catalog.EditSkillCommand.ExecuteAsync();

        Assert.Equal(
            ["Group 1: any one of Anatomy", "Group 2: any one of Biology, Chemistry"],
            dialogs.LastSkillEditor!.PrerequisiteGroups.Select(group =>
                $"{group.Heading} {string.Join(", ", group.Members.Select(member => member.Skill.Name))}"));
    }

    [Fact]
    public async Task ArchivedPrerequisiteShowsAWarning()
    {
        var api = new FakeCatalogApi();
        var medicine = api.AddTalent("Medicine");
        var leeches = api.AddSkill("Leeches");
        leeches.IsArchived = true;
        api.AddSkill("Bloodletting", medicine).PrerequisiteGroups.Add([leeches.Id]);
        var dialogs = new FakeCatalogDialogs();
        var catalog = await LoadWithSelectedSkillAsync(api, dialogs);

        await catalog.EditSkillCommand.ExecuteAsync();

        Assert.Equal("These prerequisites are archived: Leeches", dialogs.LastSkillEditor!.ArchivedPrerequisiteWarning);
    }

    [Fact]
    public async Task NewSkillSendsEveryChoiceFromTheDialog()
    {
        var api = new FakeCatalogApi();
        var medicine = api.AddTalent("Medicine");
        var anatomy = api.AddSkill("Anatomy");
        var toughness = new FakeCardFlag(Guid.NewGuid(), "Toughness");
        api.CardFlags.Add(toughness);
        var dialogs = new FakeCatalogDialogs
        {
            OnSkillEditor = editor =>
            {
                editor.Name = "Surgery";
                editor.Cost = 3;
                editor.IsMultiPurchase = true;
                editor.GameFlags[1].IsSelected = true;
                editor.CardFlags[0].IsSelected = true;
                editor.NewCardFlagNames = "Steady hands\r\n\r\n Calm \n";
                editor.AddGroupCommand.Execute(null);
                var group = editor.PrerequisiteGroups[0];
                group.SelectedCandidate = group.Candidates[0];
                group.AddMemberCommand.Execute(null);
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = await LoadWithSelectedTalentAsync(api, dialogs);

        await catalog.NewSkillCommand.ExecuteAsync();

        Assert.Equal(
            new Submission(
                "create-skill",
                $$"""{"name":"Surgery","cost":3,"isMultiPurchase":true,"isAdvanced":false,"talentIds":["{{medicine.Id:D}}"],"gameFlags":["improvesHacking"],"cardFlagIds":["{{toughness.Id:D}}"],"newCardFlagNames":["Steady hands","Calm"],"prerequisiteGroups":[["{{anatomy.Id:D}}"]]}"""),
            api.Submissions.Single());
    }

    [Fact]
    public async Task EmptyCostIsRejectedWithoutContactingTheApi()
    {
        var api = new FakeCatalogApi();
        var dialogs = new FakeCatalogDialogs
        {
            OnSkillEditor = editor =>
            {
                editor.Name = "Surgery";
                editor.Cost = null;
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();

        await catalog.NewSkillCommand.ExecuteAsync();

        Assert.Equal(
            new { Error = (string?)"Enter a cost from 0 to 999.", Submissions = 0 },
            new { Error = dialogs.LastSkillEditor!.ErrorMessage, Submissions = api.Submissions.Count });
    }

    [Fact]
    public async Task StaleSkillEditReloadsTheLatestValues()
    {
        var api = new FakeCatalogApi();
        var medicine = api.AddTalent("Medicine");
        var skill = api.AddSkill("First aid", medicine);
        var dialogs = new FakeCatalogDialogs
        {
            OnSkillEditor = editor =>
            {
                skill.Cost = 7;
                skill.Version = 2;
                editor.Name = "Bandaging";
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = await LoadWithSelectedSkillAsync(api, dialogs);

        await catalog.EditSkillCommand.ExecuteAsync();

        Assert.Equal(
            new { Name = "First aid", Cost = (decimal?)7m, Error = (string?)"Someone else changed this item. Reload it and try again." },
            new { dialogs.LastSkillEditor!.Name, dialogs.LastSkillEditor.Cost, Error = dialogs.LastSkillEditor.ErrorMessage });
    }

    [Fact]
    public async Task SavedSkillAppearsInTheSelectedTalent()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine");
        var dialogs = new FakeCatalogDialogs
        {
            OnSkillEditor = editor =>
            {
                editor.Name = "First aid";
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = await LoadWithSelectedTalentAsync(api, dialogs);

        await catalog.NewSkillCommand.ExecuteAsync();

        Assert.Equal(["First aid"], catalog.Skills.Select(skill => skill.Name));
    }

    [Fact]
    public async Task ExpiredSessionIsReported()
    {
        var api = new FakeCatalogApi { GetFailure = new ManagementApiException("errors.unauthorized", HttpStatusCode.Unauthorized) };
        var catalog = CreateCatalog(api);
        var expired = false;
        catalog.SessionExpired += (_, _) => expired = true;

        await catalog.LoadAsync();

        Assert.True(expired);
    }

    public static TheoryData<Exception, string> LoadFailures => new()
    {
        { new ManagementApiException("errors.forbidden", HttpStatusCode.Forbidden), "You are not allowed to perform this action." },
        { new HttpRequestException(), "The network service is unavailable" },
        { new System.Text.Json.JsonException(), "Something went wrong." },
        { new InvalidOperationException(), "This action uses fields or a request format this client does not support." },
        { new UriFormatException(), "Enter a valid value." }
    };

    [Theory]
    [MemberData(nameof(LoadFailures))]
    public async Task LoadFailureIsShownAsStatus(Exception failure, string expectedMessage)
    {
        var catalog = CreateCatalog(new FakeCatalogApi { GetFailure = failure });

        await catalog.LoadAsync();

        Assert.Equal(expectedMessage, catalog.StatusMessage);
    }

    [Fact]
    public void ChangingTheLanguageRelabelsTheScreen()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Old ways", isArchived: true);
        var catalog = CreateCatalog(api);
        catalog.ShowArchivedTalents = true;

        catalog.Locale = "it";

        Assert.Equal(
            new { Heading = "Attitudini", Talent = "Old ways (Archiviata)" },
            new { Heading = catalog.TalentsHeading, Talent = catalog.Talents.Single().DisplayName });
    }

    private static TalentCatalogViewModel CreateCatalog(FakeCatalogApi api, FakeCatalogDialogs? dialogs = null) =>
        new(api, new JsonLocalizer(), dialogs ?? new FakeCatalogDialogs(), "http://localhost:5000/api", "en", FakeCatalogApi.TalentsHref);

    private static async Task<TalentCatalogViewModel> LoadWithSelectedTalentAsync(FakeCatalogApi api, FakeCatalogDialogs dialogs)
    {
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();
        catalog.SelectedTalent = catalog.Talents[0];
        return catalog;
    }

    private static async Task<TalentCatalogViewModel> LoadWithSelectedSkillAsync(FakeCatalogApi api, FakeCatalogDialogs dialogs)
    {
        var catalog = await LoadWithSelectedTalentAsync(api, dialogs);
        catalog.SelectedSkill = catalog.Skills[0];
        return catalog;
    }

    private static Task SaveAs(CatalogEditorViewModel editor, string name)
    {
        editor.Name = name;
        return editor.SaveCommand.ExecuteAsync();
    }

    private static string[] SelectedValues(IEnumerable<SelectableOption> options) =>
        options.Where(option => option.IsSelected).Select(option => option.Value).ToArray();
}
