using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Skills;

public sealed class SkillEditorViewModelTests
{
    [Fact]
    public async Task AChosenSkillCannotBeAddedToTheSameGroupTwice()
    {
        var api = new FakeCatalogApi();
        api.AddSkill("Anatomy");
        var editor = await OpenNewSkillEditorAsync(api);
        editor.AddGroupCommand.Execute(null);
        var group = editor.PrerequisiteGroups[0];
        group.SelectedCandidate = group.Candidates[0];
        group.AddMemberCommand.Execute(null);

        group.SelectedCandidate = group.Candidates[0];

        Assert.False(group.AddMemberCommand.CanExecute(null));
    }

    [Fact]
    public async Task RemovingAGroupRenumbersTheOthers()
    {
        var editor = await OpenNewSkillEditorAsync(new FakeCatalogApi());
        editor.AddGroupCommand.Execute(null);
        editor.AddGroupCommand.Execute(null);

        editor.PrerequisiteGroups[0].RemoveCommand.Execute(null);

        Assert.Equal(["Group 1: any one of"], editor.PrerequisiteGroups.Select(group => group.Heading));
    }

    [Fact]
    public async Task RemovingTheArchivedPrerequisiteClearsTheWarning()
    {
        var api = new FakeCatalogApi();
        api.AddSkill("Leeches").IsArchived = true;
        var editor = await OpenNewSkillEditorAsync(api);
        editor.AddGroupCommand.Execute(null);
        var group = editor.PrerequisiteGroups[0];
        group.SelectedCandidate = group.Candidates[0];
        group.AddMemberCommand.Execute(null);

        group.Members[0].RemoveCommand.Execute(null);

        Assert.False(editor.HasArchivedPrerequisiteWarning);
    }

    [Fact]
    public async Task AddingAnArchivedPrerequisiteShowsTheWarning()
    {
        var api = new FakeCatalogApi();
        api.AddSkill("Leeches").IsArchived = true;
        var editor = await OpenNewSkillEditorAsync(api);
        editor.AddGroupCommand.Execute(null);
        var group = editor.PrerequisiteGroups[0];
        group.SelectedCandidate = group.Candidates[0];

        group.AddMemberCommand.Execute(null);

        Assert.True(editor.HasArchivedPrerequisiteWarning);
    }

    [Fact]
    public async Task FractionalCostIsRejected()
    {
        var api = new FakeCatalogApi();
        var editor = await OpenNewSkillEditorAsync(api);
        editor.Name = "Surgery";
        editor.Cost = 1.5m;

        await editor.SaveCommand.ExecuteAsync();

        Assert.Equal("Enter a cost from 0 to 999.", editor.ErrorMessage);
    }

    [Fact]
    public async Task SuccessfulSaveClosesTheDialog()
    {
        var editor = await OpenNewSkillEditorAsync(new FakeCatalogApi());
        editor.Name = "Surgery";
        var closed = false;
        editor.CloseRequested += (_, _) => closed = true;

        await editor.SaveCommand.ExecuteAsync();

        Assert.True(closed);
    }

    [Fact]
    public async Task CancelClosesTheDialogWithoutSaving()
    {
        var editor = await OpenNewSkillEditorAsync(new FakeCatalogApi());
        var closed = false;
        editor.CloseRequested += (_, _) => closed = true;

        editor.CancelCommand.Execute(null);

        Assert.Equal(new { Closed = true, Saved = false }, new { Closed = closed, Saved = editor.IsSaved });
    }

    [Fact]
    public async Task DialogLabelsAreLocalized()
    {
        var editor = await OpenNewSkillEditorAsync(new FakeCatalogApi(), "it");

        Assert.Equal(
            new
            {
                Title = "Nuova abilità",
                Cost = "Costo",
                Talents = "Attitudini",
                Prerequisites = "Prerequisiti",
                AddGroup = "Aggiungi gruppo",
                Save = "Salva"
            },
            new
            {
                editor.Title,
                Cost = editor.CostLabel,
                Talents = editor.TalentsLabel,
                Prerequisites = editor.PrerequisitesLabel,
                AddGroup = editor.AddGroupLabel,
                Save = editor.SaveLabel
            });
    }

    private static async Task<SkillEditorViewModel> OpenNewSkillEditorAsync(FakeCatalogApi api, string locale = "en")
    {
        var dialogs = new FakeCatalogDialogs();
        var catalog = new TalentCatalogViewModel(
            api,
            new JsonLocalizer(),
            dialogs,
            "http://localhost:5000/api",
            locale,
            FakeCatalogApi.TalentsHref);
        await catalog.LoadAsync();
        await catalog.NewSkillCommand.ExecuteAsync();
        return dialogs.LastSkillEditor!;
    }
}
