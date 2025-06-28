namespace BugPrince.ItemSyncInterop;

internal interface IIdentifiedRequest
{
    int RequestingPlayerID { get; set; }
    int Nonce { get; set; }
}
