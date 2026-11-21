using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>The experience dialog: a whole-number change, positive or negative, for one participant.</summary>
public sealed class ExperienceEditorViewModel : CatalogEditorViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<ExperienceEditorViewModel, Task<bool>> _submit;
    private string _amount = string.Empty;

    internal ExperienceEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        ParticipantListItem participant,
        Func<ExperienceEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, "events.changeExperience")
    {
        Participant = participant;
        _submit = submit;
    }

    public ParticipantListItem Participant { get; }

    public string Summary =>
        string.Format(CultureInfo.CurrentCulture, Translate("events.experienceSummary"),
            Participant.CharacterName, Participant.ExperiencePoints, Participant.ExperienceTotal);

    public string AmountLabel => Translate("events.experienceChange");

    public string AmountHelp => Translate("events.experienceChangeHelp");

    public string Amount
    {
        get => _amount;
        set
        {
            if (_amount == value)
            {
                return;
            }

            _amount = value;
            OnPropertyChanged();
        }
    }

    internal JsonElement CreateBody(int amount) =>
        JsonSerializer.SerializeToElement(new { characterId = Participant.CharacterId, amount }, JsonOptions);

    internal bool TryReadAmount(out int amount) =>
        int.TryParse(Amount.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount) && amount != 0;

    private protected override async Task<bool> SubmitAsync()
    {
        if (!TryReadAmount(out _))
        {
            ErrorMessage = Translate("errors.invalidExperienceChange");
            return false;
        }

        return await _submit(this);
    }
}
