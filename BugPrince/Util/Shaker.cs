using BugPrince.UI;
using UnityEngine;

namespace BugPrince.Util;

internal class Shaker : MonoBehaviour
{
    private float span = UIConstants.SHAKE_SPAN;
    private float timer;

    internal void SetSpan(float span) => this.span = span;

    internal void Shake() => timer = UIConstants.SHAKE_TIME;

    private void Update()
    {
        if (timer <= 0) return;

        timer -= Time.deltaTime;
        if (timer <= 0) transform.localPosition = Vector3.zero;
        else transform.localPosition = new(Random.Range(-span, span), Random.Range(-span, span));
    }
}

internal static class ShakerExtensions
{
    internal static Shaker AddNewChildShaker(this GameObject self, float span = UIConstants.SHAKE_SPAN)
    {
        var shaker = self.AddNewChild("Shaker").AddComponent<Shaker>();
        shaker.SetSpan(span);
        return shaker;
    }
}
