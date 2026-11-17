using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.RunningNumberEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players;

public partial class RunningNumberProjection : SingleStreamProjection<RunningNumberDocument, Guid>
{
    public RunningNumberProjection()
    {
        IncludeType<RunningNumberIssued>();
    }

    public override RunningNumberDocument? Evolve(RunningNumberDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            RunningNumberIssued issued => new RunningNumberDocument(id, issued.Number),
            _ => snapshot
        };
}
