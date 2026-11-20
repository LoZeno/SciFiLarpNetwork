namespace SciFiLarpNetwork.Backend.Application.CardTemplates;

public enum CardTemplateOutcome
{
    Succeeded,
    TemplateTooLarge,
    InvalidTemplate,
    EmptyTemplate,
    StaleVersion,
    PersistenceFailed
}
