using BugPrince.Scripts.InternalLib;
using BugPrince.Scripts.Proxy;
using PurenailCore.GOUtil;
using PurenailCore.SystemUtil;
using UnityEngine;

namespace BugPrince.Scripts.Framework;

[Shim]
internal class SecretMask : MonoBehaviour
{
    private SpriteRenderer[] masks = [];

    [ShimField] public HeroDetectorProxy? Proxy;
    [ShimField] public float RevealTime;
    [ShimField] public float ConcealDelay;
    [ShimField] public float ConcealTime;
    [ShimField] public bool Inverted;

    private void Awake() => masks = gameObject.GetComponentsInChildren<SpriteRenderer>();

    private float delay;
    private float alpha = 1;

    private void Update()
    {
        if (Proxy == null) return;

        if (Proxy.Detected())
        {
            alpha -= Time.deltaTime / RevealTime;
            if (alpha < 0) alpha = 0;
            delay = ConcealDelay;
        }
        else
        {
            delay -= Time.deltaTime;
            if (delay < 0)
            {
                alpha += -delay / ConcealTime;
                if (alpha > 1) alpha = 1;
                delay = 0;
            }
        }

        masks.ForEach(m => m.SetAlpha(Inverted ? 1 - alpha : alpha));
    }
}
