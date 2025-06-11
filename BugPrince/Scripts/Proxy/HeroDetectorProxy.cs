using BugPrince.Scripts.InternalLib;
using System;
using UnityEngine;

namespace BugPrince.Scripts.Proxy;

[Shim]
internal class HeroDetectorProxy : MonoBehaviour
{
    private event Action? OnDetectedEvent;
    private event Action? OnUndetectedEvent;

    private int detected = 0;
    private bool prevDetected = false;

    public bool Detected() => detected > 0;

    private void OnTriggerEnter2D(Collider2D collider) => ++detected;

    private void OnTriggerExit2D(Collider2D collider) => --detected;

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
