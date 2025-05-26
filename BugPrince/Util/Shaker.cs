using UnityEngine;

namespace BugPrince.Util;

internal class Shaker : MonoBehaviour
{
    private float xRange;
    private float yRange;
    private float duration;

    private float timer;

    internal void Init(float xRange, float yRange, float duration)
    {
        this.xRange = xRange;
        this.yRange = yRange;
        this.duration = duration;
    }

    internal void Shake() => timer = duration;

    private void Update()
    {
        if (timer <= 0) return;

        timer -= Time.deltaTime;
        if (timer <= 0) transform.localPosition = Vector3.zero;

        transform.localPosition = new(Random.Range(-xRange, xRange), Random.Range(-yRange, yRange));
    }
}

internal static class ShakerExtensions
{
    internal static Shaker AddShakerChild(this GameObject self, float xRange, float yRange, float duration)
    {
        GameObject child = new("Shaker");
        child.transform.SetParent(self.transform);

        var shaker = child.AddComponent<Shaker>();
        shaker.Init(xRange, yRange, duration);
        child.SetActive(true);
        return shaker;
    }
    internal static Shaker AddShakerChild(this GameObject self, float range, float duration) => self.AddShakerChild(range, range, duration);
}
