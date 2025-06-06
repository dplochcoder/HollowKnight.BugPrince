﻿using UnityEngine;

namespace BugPrince.UI;

internal class PinAnimator : MonoBehaviour
{
    private bool show = false;

    private SpriteRenderer? spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = new(1, 1, 1, 0);
    }

    internal void Pin(bool show) => this.show = show;

    private float pinTimer = 0;

    private void Update()
    {
        if (show && pinTimer < UIConstants.PIN_ANIMATION_TIME) pinTimer = Mathf.Min(UIConstants.PIN_ANIMATION_TIME, pinTimer + Time.deltaTime);
        else if (!show && pinTimer > 0) pinTimer = Mathf.Max(0, pinTimer - Time.deltaTime);

        spriteRenderer!.color = new(1, 1, 1, pinTimer / UIConstants.PIN_ANIMATION_TIME);

        var invPct = 1 - (pinTimer / UIConstants.PIN_ANIMATION_TIME);
        var scale = Mathf.Pow(UIConstants.PIN_ANIMATION_SCALE, invPct) * UIConstants.PIN_ICON_SCALE;
        transform.localScale = new(scale, scale, scale);
        transform.localRotation = Quaternion.Euler(0, 0, 180f * (1 + pinTimer / UIConstants.PIN_ANIMATION_TIME));
    }
}
