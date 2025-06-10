using BugPrince.Scripts.InternalLib;
using UnityEngine;

namespace BugPrince.Scripts.Framework;

[Shim]
internal class DarknessManager : MonoBehaviour
{
    [ShimField] public int DarknessLevel;

    private int sceneDarkness;
    private PlayMakerFSM? vignette;

    private void Awake()
    {
        sceneDarkness = GameObject.FindGameObjectWithTag("SceneManager").GetComponent<SceneManager>().GetDarknessLevel();
        vignette = GameObject.FindGameObjectWithTag("Vignette").LocateMyFSM("Darkness Control");
    }

    private int triggerCount;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (++triggerCount > 1) return;

        HeroController.instance.SetDarkness(DarknessLevel);
        vignette!.FsmVariables.GetFsmInt("Darkness Level").Value = DarknessLevel;
        vignette!.SendEvent("SCENE RESET");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (--triggerCount > 0) return;

        HeroController.instance.SetDarkness(sceneDarkness);
        vignette!.FsmVariables.GetFsmInt("Darkness Level").Value = sceneDarkness;
        vignette!.SendEvent("SCENE RESET");
    }
}
