namespace BugPrince.ItemSyncInterop;

public record SwapTransitionsRequest : IIdentifiedRequest
{
    public int RequestingPlayerID { get; set; }
    public int Nonce { get; set; }
    public TransitionSyncUpdate Update = new();
}
