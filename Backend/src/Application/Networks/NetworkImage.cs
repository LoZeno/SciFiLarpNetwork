namespace SciFiLarpNetwork.Backend.Application.Networks;

public sealed record NetworkImage(Guid ImageId, string MediaType, byte[] Content);
