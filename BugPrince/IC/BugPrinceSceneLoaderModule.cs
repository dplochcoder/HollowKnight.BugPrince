using BugPrince.Scripts.Proxy;
using ItemChanger;
using Modding;
using PurenailCore.ICUtil;
using PurenailCore.SystemUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC;

internal class BugPrinceSceneLoaderModule : ItemChanger.Modules.Module
{
    public bool VisitedGemstoneCavern;
    public bool VisitedTheVault;

    private readonly Dictionary<string, AssetBundle?> sceneBundles = [];
    private SceneLoaderModule? coreModule;

    private const string GEMSTONE_CAVERN = "GEMSTONE_CAVERN";
    private const int GEMSTONE_CAVERN_ID = 123499;
    private const string THE_VAULT = "THE_VAULT";
    private const int THE_VAULT_ID = 123488;

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

        ModHooks.LanguageGetHook += LanguageGetHook;
        ModHooks.LanguageGetHook += LanguageGetHook;
        ModHooks.GetPlayerBoolHook += GetPlayerBoolHook;
        Events.AddSceneChangeEdit("BugPrince_GemstoneCavern", ShowGemstoneCavernTitle);
        Events.AddSceneChangeEdit("BugPrince_Vault", ShowVaultTitle);
    }

    public override void Unload()
    {
        sceneBundles.Values.ForEach(v => v?.Unload(true));
        coreModule!.RemoveOnBeforeSceneLoad(OnBeforeSceneLoad);
        coreModule.RemoveOnUnloadScene(OnUnloadScene);

        ModHooks.LanguageGetHook -= LanguageGetHook;
        ModHooks.GetPlayerBoolHook -= GetPlayerBoolHook;
        ModHooks.SetPlayerBoolHook -= SetPlayerBoolHook;
        Events.RemoveSceneChangeEdit("BugPrince_GemstoneCavern", ShowGemstoneCavernTitle);
        Events.RemoveSceneChangeEdit("BugPrince_Vault", ShowVaultTitle);
    }

    private void ShowGemstoneCavernTitle(Scene scene) => AreaTitleControllerProxy.ShowAreaTitle(GEMSTONE_CAVERN, GEMSTONE_CAVERN_ID, nameof(VisitedGemstoneCavern));
    private void ShowVaultTitle(Scene scene) => AreaTitleControllerProxy.ShowAreaTitle(THE_VAULT, THE_VAULT_ID, nameof(VisitedTheVault));

    private string LanguageGetHook(string key, string sheetTitle, string orig)
    {
        return key switch
        {
            $"{GEMSTONE_CAVERN}_SUPER" => "",
            $"{GEMSTONE_CAVERN}_MAIN" => "Gemstone Cavern",
            $"{GEMSTONE_CAVERN}_SUB" => "",
            $"{THE_VAULT}_SUPER" => "",
            $"{THE_VAULT}_MAIN" => "The Vault",
            $"{THE_VAULT}_SUB" => "",
            _ => orig
        };
    }

    private bool GetPlayerBoolHook(string name, bool orig)
    {
        return name switch
        {
            nameof(VisitedGemstoneCavern) => VisitedGemstoneCavern,
            nameof(VisitedTheVault) => VisitedTheVault,
            _ => orig
        };
    }

    private bool SetPlayerBoolHook(string name, bool value)
    {
        return name switch
        {
            nameof(VisitedGemstoneCavern) => (VisitedGemstoneCavern = value),
            nameof(VisitedTheVault) => (VisitedTheVault = value),
            _ => value,
        };
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
