using ItemChanger.Extensions;
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
