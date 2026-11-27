namespace SciFiLarpNetwork.Backend.Application.Items;

public sealed record ItemImage(Guid ImageId, string MediaType, byte[] Content);
