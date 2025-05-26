using UnityEngine;

namespace BugPrince.Util;

internal static class FadeUtil
{
    internal static void FadeColor(this GameObject self, Color from, Color to, float duration)
    {
        self.GetComponent<SpriteRenderer>().color = from;

        var fade = self.GetComponent<SimpleSpriteFade>();
        if (fade != null) Object.Destroy(fade);

        fade = self.AddComponent<SimpleSpriteFade>();
        fade.fadeInColor = to;
        fade.fadeDuration = duration;
        fade.FadeIn();
    }
}
