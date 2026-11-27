using SciFiLarpNetwork.Backend.Application.News;
using SciFiLarpNetwork.Backend.Infrastructure.Administration;
using SciFiLarpNetwork.Backend.Infrastructure.News.NewsDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.News;

internal static class NewsMappings
{
    extension(NewsMediaDocument media)
    {
        public NewsMediaSummary ToSummary(AdministratorNames administrators) =>
            new(media.Id, media.Name, media.MediaType, media.Kind, media.ByteCount, media.Width, media.Height,
                media.UploadedAtUtc, administrators.Describe(media.UploadedBy));
    }
}
