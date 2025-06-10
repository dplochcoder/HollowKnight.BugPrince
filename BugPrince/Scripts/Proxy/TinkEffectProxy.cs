using BugPrince.Scripts.InternalLib;
using SFCore.Utils;
using UnityEngine;

namespace BugPrince.Scripts.Proxy;

[Shim]
internal class TinkEffectProxy : TinkEffect
{
    [ShimField] public bool UseNailPosition;

    private void Awake()
    {
        useNailPosition = UseNailPosition;
        blockEffect = BugPrincePreloader.Instance.Goam!.GetComponent<TinkEffect>().blockEffect;
        this.SetAttr<TinkEffect, BoxCollider2D>("boxCollider", gameObject.GetComponent<BoxCollider2D>());
    }
}
