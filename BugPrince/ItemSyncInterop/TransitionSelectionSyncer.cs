using BugPrince.IC;
using ItemChanger.Modules;
using ItemSyncMod;
using JsonUtil = PurenailCore.SystemUtil.JsonUtil<BugPrince.BugPrinceMod>;
using MultiWorldLib;
using System;
using System.Collections.Generic;
using System.IO;
using ItemChanger;

namespace BugPrince.ItemSyncInterop;

// Whoever has player id 0 is considered the host and all of their decisions are authoritative.
// Other players can send write requests to the host, who can either confirm or reject them.
// On confirmation, the write is immediately made canonical and broadcast to all other players.
internal class TransitionSelectionSyncer : Module
{
    private readonly Dictionary<string, Action<DataReceivedEvent>> handlers = [];

    private static void HandlerImpl<T>(DataReceivedEvent data, Action<T> handler) where T : class, new()
    {
        T message = new();
        try
        {
            message = JsonUtil.DeserializeFromString<T>(data.Content);
            BugPrinceMod.DebugLog($"RECEIVED {typeof(T).Name}: {data.Content}");
        }
        catch (Exception ex)
        {
            BugPrinceMod.Instance!.LogError($"Deserialize error: {typeof(T).Name}: {data.Content}");
            BugPrinceMod.Instance.LogError($"{ex}");
        }

        try
        {
            handler(message);
        }
        catch (Exception ex)
        {
            BugPrinceMod.Instance!.LogError($"Error handling {typeof(T).Name}: {data.Content}");
            BugPrinceMod.Instance.LogError($"{ex}");
        }
    }

    private static string Label<T>() => $"BugPrince-{typeof(T).Name}";

    private void AddHandler<T>(Action<T> handler) where T : class, new() => handlers.Add(Label<T>(), data => HandlerImpl(data, handler));

    private static TransitionSelectionSyncer? instance;

    internal static TransitionSelectionSyncer? Get() => instance;

    private static ClientConnection ISConnection => ItemSyncMod.ItemSyncMod.Connection;

    public override void Initialize()
    {
        AddHandler<SwapTransitionsRequest>(Handle);
        AddHandler<SwapTransitionsResponse>(Handle);
        AddHandler<TransitionSwapUpdate>(Handle);
        AddHandler<TransitionSwapUpdates>(Handle);
        AddHandler<GetTransitionSwapUpdatesRequest>(Handle);
        AddHandler<GetTransitionSwapUpdatesResponse>(Handle);

        ISConnection.OnDataReceived += HandleMessages;

        instance = this;
    }

    public override void Unload()
    {
        instance = null;

        ISConnection.OnDataReceived -= HandleMessages;
    }

    private void HandleMessages(DataReceivedEvent data)
    {
        if (!handlers.TryGetValue(data.Label, out var handler)) return;
        
        handler(data);
        data.Handled = true;
    }

    private bool IsHost => ItemSyncMod.ItemSyncMod.ISSettings.MWPlayerId == 0;

    private bool MoreThanTwo => ISConnection.GetConnectedPlayers().Count > 2;

    private const int TTL = 300;

    private void SendData<T>(T data, int to) where T : class
    {
        StringWriter sw = new();
        JsonUtil.Serialize(data, sw);
        BugPrinceMod.DebugLog($"SENT {typeof(T).Name} to {to}: {sw}");
        ISConnection.SendData(Label<T>(), sw.ToString(), to, TTL);
    }

    private void SendDataToAll<T>(T data) where T : class
    {
        StringWriter sw = new();
        JsonUtil.Serialize(data, sw);
        BugPrinceMod.DebugLog($"SENT {typeof(T).Name} to all: {sw}");
        ISConnection.SendDataToAll(Label<T>(), sw.ToString(), TTL);
    }

    internal void SendImpl<Req, Resp>(Dictionary<int, Action<Resp>> callbacks, Req request, Action<Resp> callback) where Req : class, IIdentifiedRequest
    {
        request.RequestingPlayerID = ItemSyncMod.ItemSyncMod.ISSettings.MWPlayerId;
        do
        {
            request.Nonce = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        } while (callbacks.ContainsKey(request.Nonce));
        callbacks.Add(request.Nonce, callback);

        SendData(request, 0);
    }

    private void HandleResponseImpl<T>(Dictionary<int, Action<T>> callbacks, T response) where T : IIdentifiedRequest
    {
        if (IsHost) return;
        if (!callbacks.TryGetValue(response.Nonce, out var callback)) return;

        callbacks.Remove(response.Nonce);
        callback(response);
    }

    private readonly Dictionary<int, Action<SwapTransitionsResponse>> swapTransitionsCallbacks = [];

    private void Handle(SwapTransitionsRequest request)
    {
        if (!IsHost) return;

        TransitionSelectionModule.Get()!.SwapTransitions(request, response =>
        {
            SendData(response, request.RequestingPlayerID);
            if (MoreThanTwo) SendDataToAll(response.Updates);
        });
    }

    private void Handle(SwapTransitionsResponse response) => HandleResponseImpl(swapTransitionsCallbacks, response);

    internal void Send(SwapTransitionsRequest request, Action<SwapTransitionsResponse> callback) => SendImpl(swapTransitionsCallbacks, request, callback);

    private void Handle(TransitionSwapUpdate update)
    {
        if (IsHost) return;
        TransitionSelectionModule.Get()!.ApplyTransitionSwapUpdates([update]);
    }

    internal void Send(TransitionSwapUpdate update)
    {
        if (!IsHost) return;
        SendDataToAll(update);
    }

    private void Handle(TransitionSwapUpdates updates)
    {
        if (IsHost) return;
        TransitionSelectionModule.Get()!.ApplyTransitionSwapUpdates(updates.Updates);
    }

    internal void Send(TransitionSwapUpdates updates)
    {
        if (!IsHost) return;
        SendDataToAll(updates);
    }

    private readonly Dictionary<int, Action<GetTransitionSwapUpdatesResponse>> getTransitionUpdatesCallbacks = [];

    private void Handle(GetTransitionSwapUpdatesRequest request)
    {
        if (!IsHost) return;
        TransitionSelectionModule.Get()!.GetTransitionSwapUpdates(request, response => SendData(response, request.RequestingPlayerID));
    }

    private void Handle(GetTransitionSwapUpdatesResponse response) => HandleResponseImpl(getTransitionUpdatesCallbacks, response);

    internal void Send(GetTransitionSwapUpdatesRequest request, Action<GetTransitionSwapUpdatesResponse> callback) => SendImpl(getTransitionUpdatesCallbacks, request, callback);
}
