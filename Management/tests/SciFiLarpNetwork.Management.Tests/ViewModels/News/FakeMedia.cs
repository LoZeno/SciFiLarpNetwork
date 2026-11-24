namespace SciFiLarpNetwork.Management.Tests.ViewModels.News;

internal sealed record FakeMedia(Guid Id, string Name, string MediaType, bool IsVideo, long ByteCount, DateTimeOffset UploadedAt);
