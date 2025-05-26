using UnityEngine;

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

    private const float PIN_TIME = 0.6f;
    private float pinTimer = 0;

    private void Update()
    {
        if (show && pinTimer < PIN_TIME) pinTimer = Mathf.Min(PIN_TIME, pinTimer + Time.deltaTime);
        else if (!show && pinTimer > 0) pinTimer = Mathf.Max(0, pinTimer - Time.deltaTime);

        spriteRenderer!.color = new(1, 1, 1, pinTimer / PIN_TIME);

        var invPct = 1 - (pinTimer / PIN_TIME);
        var scale = Mathf.Pow(3, invPct);
        transform.localScale = new(scale, scale, scale);
        transform.localRotation = Quaternion.Euler(0, 0, pinTimer * 360f / PIN_TIME);
    }
}
