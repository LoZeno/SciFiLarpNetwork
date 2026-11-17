using System.Text.Json;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Management.Api.Players;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <summary>
/// The player dialog. <see cref="CatalogEditorViewModel.Name"/> holds the display name and
/// <see cref="CatalogEditorViewModel.Description"/> the notes.
/// </summary>
public sealed class PlayerEditorViewModel : CatalogEditorViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly Func<PlayerEditorViewModel, Task<bool>> _submit;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private RosterChoice? _selectedType;

    internal PlayerEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        string titleKey,
        IReadOnlyList<RosterChoice> types,
        Func<PlayerEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, titleKey)
    {
        Types = types;
        _selectedType = types.Count > 0 ? types[0] : null;
        _submit = submit;
    }

    public IReadOnlyList<RosterChoice> Types { get; }

    public string DisplayNameLabel => Translate("players.displayName");

    public string EmailLabel => Translate("players.email");

    public string PhoneLabel => Translate("players.phone");

    public string NotesLabel => Translate("players.notes");

    public string TypeLabel => Translate("players.type");

    public string Email
    {
        get => _email;
        set
        {
            if (_email == value)
            {
                return;
            }

            _email = value;
            OnPropertyChanged();
        }
    }

    public string Phone
    {
        get => _phone;
        set
        {
            if (_phone == value)
            {
                return;
            }

            _phone = value;
            OnPropertyChanged();
        }
    }

    public RosterChoice? SelectedType
    {
        get => _selectedType;
        set
        {
            if (_selectedType == value)
            {
                return;
            }

            _selectedType = value;
            OnPropertyChanged();
        }
    }

    internal void Apply(PlayerProperties player)
    {
        Name = player.DisplayName;
        Email = player.Email ?? string.Empty;
        Phone = player.Phone ?? string.Empty;
        Description = player.Notes ?? string.Empty;
        SelectedType = Types.FirstOrDefault(type => type.Name == player.Type) ?? SelectedType;
    }

    internal JsonElement CreateBody(long? version) =>
        JsonSerializer.SerializeToElement(
            new
            {
                version,
                displayName = Name,
                email = Blank(Email),
                phone = Blank(Phone),
                notes = Blank(Description),
                type = SelectedType?.Name
            },
            JsonOptions);

    private protected override Task<bool> SubmitAsync() => _submit(this);

    private static string? Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
