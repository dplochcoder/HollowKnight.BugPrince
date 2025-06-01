using GlobalEnums;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.Internal;
using ItemChanger.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BugPrince.Util;

internal static class GameObjectUtil
{
    internal static GameObject AddNewChild(this GameObject self, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(self.transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child;
    }

    internal static List<GameObject> Children(this GameObject self)
    {
        List<GameObject> ret = [];
        foreach (Transform transform in self.transform) ret.Add(transform.gameObject);
        return ret;
    }

    internal static void Recursively(this GameObject self, Action<GameObject> action)
    {
        Queue<GameObject> queue = new([self]);
        while (queue.Count > 0)
        {
            var obj = queue.Dequeue();
            action(obj);

            foreach (Transform transform in obj.transform) queue.Enqueue(transform.gameObject);
        }
    }

    internal static void DoOnDestroy(this GameObject self, Action action) => self.GetOrAddComponent<OnDestroyHook>().Action += action;

    internal static void FadeColor(this GameObject self, Color from, Color to, float duration)
    {
        if (self.GetComponent<SpriteRenderer>() == null && self.GetComponent<TextMesh>() == null) return;
        self.GetOrAddComponent<Fader>().StartFade(from, to, duration);
    }

    internal static void FadeColor(this GameObject self, Color to, float duration)
    {
        if (self.GetComponent<SpriteRenderer>() is SpriteRenderer sr) self.GetOrAddComponent<Fader>().StartFade(sr.color, to, duration);
        if (self.GetComponent<TextMesh>() is TextMesh tm) self.GetOrAddComponent<Fader>().StartFade(tm.color, to, duration);
    }

    internal static GameObject MakeShinyDecorator()
    {
        var obj = ObjectCache.ShinyItem;
        UnityEngine.Object.Destroy(obj.FindChild("Inspect Region")!);
        UnityEngine.Object.Destroy(obj.FindChild("White Wave Region")!);
        UnityEngine.Object.Destroy(obj.GetComponent<PersistentBoolItem>());
        UnityEngine.Object.Destroy(obj.GetComponent<Rigidbody2D>());
        UnityEngine.Object.Destroy(obj.LocateMyFSM("Shiny Control"));
        UnityEngine.Object.Destroy(obj.LocateMyFSM("Generate Wave"));
        return obj;
    }

    internal static GameObject MakeShinyDecorator(IEnumerable<AbstractItem> items)
    {
        var obj = MakeShinyDecorator();
        ShinyUtility.SetShinyColor(obj, items);
        return obj;
    }

    internal static GameObject FlingGlassDebris(Vector3 pos, Vector3 speed)
    {
        var obj = UnityEngine.Object.Instantiate(BugPrincePreloader.Instance.QuakeFloorGlassDebris.Random());
        obj.transform.position = pos;
        obj.GetComponent<Rigidbody2D>().velocity = speed;
        return obj;
    }
}

internal class Fader : MonoBehaviour
{
    private Color from;
    private Color to;
    private float timer;
    private float duration;

    private SpriteRenderer? spriteRenderer;
    private TextMesh? textMesh;

    internal void StartFade(Color from, Color to, float duration)
    {
        this.from = from;
        this.to = to;
        this.duration = duration;
        timer = 0;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.color = from;
        textMesh = GetComponent<TextMesh>();
        if (textMesh != null) textMesh.color = from;
    }

    private void Update()
    {
        if (timer < duration)
        {
            timer += Time.deltaTime;
            if (timer > duration) timer = duration;
        }

        float pct = timer / duration;
        var color = Color.Lerp(from, to, pct);
        if (spriteRenderer != null) spriteRenderer.color = color;
        if (textMesh != null) textMesh.color = color;
    }
}

internal class OnDestroyHook : MonoBehaviour
{
    internal event Action? Action;

    private void OnDestroy() => Action?.Invoke();
}
