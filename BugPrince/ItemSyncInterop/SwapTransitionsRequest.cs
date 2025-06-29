namespace BugPrince.ItemSyncInterop;

public record SwapTransitionsRequest : IIdentifiedRequest
{
    public int RequestingPlayerID { get; set; }
    public int Nonce { get; set; }
    public TransitionSwapUpdate Update = new();
}
