namespace SciFiLarpNetwork.Backend.Domain.Characters;

/// <remarks>Members are persisted by name, so renaming a member is a breaking change to stored events.</remarks>
public enum CharacterStatus
{
    Active,
    Retired,
    Dead
}
