using System.Collections.Generic;

namespace BugPrince.ItemSyncInterop;

public record GetTransitionUpdatesResponse : IIdentifiedRequest
{
    public int RequestingPlayerID { get; set; }
    public int Nonce { get; set; }
    public List<TransitionSyncUpdate> Updates = [];
}
