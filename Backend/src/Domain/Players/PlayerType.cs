namespace SciFiLarpNetwork.Backend.Domain.Players;

/// <remarks>Members are persisted by name, so renaming a member is a breaking change to stored events.</remarks>
public enum PlayerType
{
    Player,
    NonPlayer,
    GameMaster,
    Senior
}
