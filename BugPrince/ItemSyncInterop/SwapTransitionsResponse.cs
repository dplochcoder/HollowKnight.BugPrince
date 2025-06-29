namespace BugPrince.ItemSyncInterop;

public class SwapTransitionsResponse : IIdentifiedRequest
{
    public int RequestingPlayerID {  get; set; }
    public int Nonce { get; set; }

    // If the update was accepted, possibly with alterations.
    public bool Accepted;
    // False if we lost the pin to a race condition.
    public bool AcceptedPin;
    // The actual updates applied. This may include new updates and/or changes to the requested update.
    public TransitionSwapUpdates Updates = new();
}
