namespace SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

/// <summary>A running-number sequence: the last number it handed out.</summary>
public sealed record RunningNumberDocument(Guid Id, int LastNumber)
{
    /// <summary>The well-known id of the player running-number stream.</summary>
    public static readonly Guid PlayerSequenceId = new("0b8f6c3e-58d1-4a2f-9c47-1e6d2a9b3f71");

    /// <summary>The well-known id of the character running-number stream.</summary>
    public static readonly Guid CharacterSequenceId = new("7d2e4a19-c6b3-4f80-8e15-5a9c0d3b6e24");

    /// <summary>The well-known id of the event running-number stream.</summary>
    public static readonly Guid EventSequenceId = new("c4a7e2d9-1b38-4f65-a0d2-8e3f5b6c7a19");

    /// <summary>The stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
