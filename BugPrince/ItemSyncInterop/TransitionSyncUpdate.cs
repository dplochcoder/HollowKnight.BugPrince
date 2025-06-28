using ItemChanger;
using System.Collections.Generic;

namespace BugPrince.ItemSyncInterop;

public record TransitionSyncUpdate
{
    public int SequenceNumber;
    public Transition source1;
    public Transition target1;
    public Transition source2;
    public Transition target2;
    public List<List<string>> RefreshCounterUpdates = [];  // One or two (if used dice totem) sets of scenes to update counters for, in order.
    public bool UsedPin;  // True if the requested used their push pin during this selection.
}
