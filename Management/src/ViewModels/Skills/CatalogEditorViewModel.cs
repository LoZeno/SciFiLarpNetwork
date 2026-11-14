using System.Diagnostics;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Skills;

/// <summary>
/// Shared state of the talent and skill dialogs. Saving keeps the dialog open and shows the error when the
/// save fails, and closes the dialog when it succeeds.
/// </summary>
public abstract class CatalogEditorViewModel : ObservableViewModel
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string? _errorMessage;
    private bool _isSaving;

    private protected CatalogEditorViewModel(IManagementLocalizer localizer, string locale, string titleKey)
    {
        Localizer = localizer;
        Locale = locale;
        Title = Translate(titleKey);
        SaveCommand = new AsyncCommand(SaveAsync, HandleUnexpectedFailure, () => !IsSaving);
        CancelCommand = new DelegateCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty), () => !IsSaving);
    }

    public event EventHandler? CloseRequested;

    public AsyncCommand SaveCommand { get; }

    public DelegateCommand CancelCommand { get; }

    public bool IsSaved { get; private set; }

    public string Title { get; }

    public string NameLabel => Translate("skills.name");

    public string DescriptionLabel => Translate("skills.description");

    public string SaveLabel => Translate("common.save");

    public string CancelLabel => Translate("common.cancel");

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description == value)
            {
                return;
            }

            _description = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage == value)
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (_isSaving == value)
            {
                return;
            }

            _isSaving = value;
            OnPropertyChanged();
            SaveCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
        }
    }

    private protected IManagementLocalizer Localizer { get; }

    private protected string Locale { get; }

    private protected string Translate(string key) => Localizer.Translate(key, Locale);

    private protected abstract Task<bool> SubmitAsync();

    private async Task SaveAsync()
    {
        IsSaving = true;
        ErrorMessage = null;
        bool saved;
        try
        {
            saved = await SubmitAsync();
        }
        finally
        {
            IsSaving = false;
        }

        if (saved)
        {
            IsSaved = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void HandleUnexpectedFailure(Exception exception)
    {
        Trace.TraceError("Unhandled catalog editor exception: {0}", exception);
        ErrorMessage = Translate("common.error");
    }
}
