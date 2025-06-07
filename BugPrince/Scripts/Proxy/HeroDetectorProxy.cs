using BugPrince.Scripts.InternalLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugPrince.Scripts.Proxy;

[Shim]
internal class HeroDetectorProxy : MonoBehaviour
{
    private event Action? OnDetectedEvent;
    private event Action? OnUndetectedEvent;

    private HashSet<Collider2D> detected = [];
    private List<Func<Collider2D, bool>> ignores = [];
    private bool prevDetected = false;

    private bool ShouldIgnore(Collider2D collider) => ignores.Any(f => f(collider));

    public void Ignore(Func<Collider2D, bool> filter)
    {
        ignores.Add(filter);
        detected.RemoveWhere(ShouldIgnore);
    }
    public void Ignore(Collider2D collider) => Ignore(c => c == collider);

    public bool Detected() => detected.Count > 0;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (ShouldIgnore(collider)) return;
        detected.Add(collider);
    }

    private void OnTriggerExit2D(Collider2D collider) => detected.Remove(collider);

    public void OnDetected(Action action)
    {
        if (Detected()) action();
        OnDetectedEvent += action;
    }

    public void Listen(Action detect, Action undetect)
    {
        if (Detected()) detect.Invoke();
        else undetect.Invoke();

        OnDetectedEvent += detect;
        OnUndetectedEvent += undetect;
    }

    private void Update()
    {
        bool newDetected = Detected();
        if (newDetected != prevDetected)
        {
            if (newDetected) OnDetectedEvent?.Invoke();
            else OnUndetectedEvent?.Invoke();

            prevDetected = newDetected;
        }
    }
}
