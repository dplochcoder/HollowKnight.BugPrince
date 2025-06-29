namespace BugPrince.ItemSyncInterop;

public record GetTransitionSwapUpdatesRequest : IIdentifiedRequest
{
    public int RequestingPlayerID { get; set; }
    public int Nonce { get; set; }
    public int LastKnownSequenceNumber;
}
