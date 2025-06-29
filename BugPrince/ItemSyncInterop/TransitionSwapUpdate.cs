using ItemChanger;
using System.Collections.Generic;

namespace BugPrince.ItemSyncInterop;

public record TransitionSwap
{
    public Transition Source1;
    public Transition Source2;
    public Transition Target2;
}

public record PinReceipt
{
    public int RequestingPlayerID;
    public int ReceiptNumber;
}

public record TransitionSwapUpdate
{
    public int SequenceNumber;
    public TransitionSwap? Swap = new();
    public List<List<string>> RefreshCounterUpdates = [];  // Sets of scenes to update counters for, in order.
    public PinReceipt? PinReceipt;  // Set if a player spent a pin on this swap attempt.
}
