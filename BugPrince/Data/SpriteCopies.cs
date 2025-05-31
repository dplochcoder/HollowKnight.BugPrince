using Modding.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace BugPrince.Data;

internal record SpriteCopy
{
    public string OrigPath = "";
    [JsonConverter(typeof(Vector3Converter))] public Vector3 Position;
    public float Rotation;
    [JsonConverter(typeof(Vector3Converter))] public Vector3 Scale;
}

internal static class SpriteCopies
{
    private static List<SpriteCopy>? lurienSecretData;
    internal static IReadOnlyList<SpriteCopy> GetLurienSecretSpriteCopies() => lurienSecretData ??= PurenailCore.SystemUtil.JsonUtil<BugPrinceMod>.DeserializeEmbedded<List<SpriteCopy>>("BugPrince.Resources.Data.lurien_secret_sprite_copies.json");
}
