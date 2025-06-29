using System.Collections.Generic;

namespace BugPrince.ItemSyncInterop;

public record GetTransitionSwapUpdatesResponse : IIdentifiedRequest
{
    public int RequestingPlayerID { get; set; }
    public int Nonce { get; set; }
    public List<TransitionSwapUpdate> Updates = [];
}
