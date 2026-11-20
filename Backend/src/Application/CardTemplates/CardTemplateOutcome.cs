namespace SciFiLarpNetwork.Backend.Application.CardTemplates;

public enum CardTemplateOutcome
{
    Succeeded,
    TemplateTooLarge,
    InvalidTemplate,
    StaleVersion,
    PersistenceFailed
}
