using ItemChanger;
using PurenailCore.ICUtil;
using PurenailCore.SystemUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BugPrince.IC;

internal class BugPrinceSceneLoaderModule : ItemChanger.Modules.Module
{
    private readonly Dictionary<string, AssetBundle?> sceneBundles = [];
    private SceneLoaderModule? coreModule;

    public override void Initialize()
    {
        foreach (var str in typeof(BugPrinceSceneLoaderModule).Assembly.GetManifestResourceNames())
        {
            if (!str.StartsWith(PREFIX) || str.EndsWith(".manifest") || str.EndsWith("meta")) continue;
            string name = str.Substring(PREFIX.Length);
            if (name == "AssetBundles" || name == "scenes") continue;

            sceneBundles[name] = null;
        }

        coreModule = ItemChangerMod.Modules.GetOrAdd<SceneLoaderModule>();
        coreModule.AddOnBeforeSceneLoad(OnBeforeSceneLoad);
        coreModule.AddOnUnloadScene(OnUnloadScene);
    }

    public override void Unload()
    {
        sceneBundles.Values.ForEach(v => v?.Unload(true));
        coreModule!.RemoveOnBeforeSceneLoad(OnBeforeSceneLoad);
        coreModule.RemoveOnUnloadScene(OnUnloadScene);
    }

    private void OnBeforeSceneLoad(string sceneName, Action cb)
    {
        var assetBundleName = AssetBundleName(sceneName);
        if (!sceneBundles.ContainsKey(assetBundleName))
        {
            cb();
            return;
        }

        GameManager.instance.StartCoroutine(LoadSceneAsync(assetBundleName, cb));
    }

    private void OnUnloadScene(string prevSceneName, string nextSceneName)
    {
        if (nextSceneName == prevSceneName) return;

        var assetBundleName = AssetBundleName(prevSceneName);
        if (sceneBundles.TryGetValue(assetBundleName, out var assetBundle))
        {
            assetBundle?.Unload(true);
            sceneBundles[assetBundleName] = null;
        }
    }

    private static string AssetBundleName(string sceneName) => sceneName.Replace("_", "").ToLower();

    private const string PREFIX = "BugPrince.Unity.Assets.AssetBundles.";

    private IEnumerator LoadSceneAsync(string assetBundleName, Action callback)
    {
        if (sceneBundles[assetBundleName] != null)
        {
            callback();
            yield break;
        }

        StreamReader sr = new(typeof(BugPrinceSceneLoaderModule).Assembly.GetManifestResourceStream($"{PREFIX}{assetBundleName}"));
        var request = AssetBundle.LoadFromStreamAsync(sr.BaseStream);
        yield return request;

        sceneBundles[assetBundleName] = request.assetBundle;
        callback();
    }
}
