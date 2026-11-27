namespace SciFiLarpNetwork.Backend.Domain.Items;

public enum ItemRecipeProblem
{
    None,
    InvalidQuantity,
    TooManyComponents,
    SelfComponent,
    DuplicateComponent
}
