using BugPrince.IC;
using UnityEngine;

namespace BugPrince.UI;

internal class PlayerLocker : MonoBehaviour
{
    private Transform? knight;
    private Rigidbody2D? rb2d;
    private Vector3 origPos;
    private Vector2 origVel;
    private GateDirection gateDir;

    private void Awake()
    {
        knight = HeroController.instance.gameObject.transform;
        origPos = knight.position;

        rb2d = knight.GetComponent<Rigidbody2D>();
        origVel = rb2d.velocity;
        rb2d.simulated = false;

        HeroController.instance.RelinquishControl();

        On.CameraController.LateUpdate += LockCamera;
    }

    private void OnDestroy()
    {
        rb2d!.simulated = true;

        On.CameraController.LateUpdate -= LockCamera;
    }

    private Vector3? origCameraPos;
    private void LockCamera(On.CameraController.orig_LateUpdate orig, CameraController self)
    {
        origCameraPos ??= self.transform.position;
        self.transform.position = origCameraPos.Value;
    }

    internal void SetDirection(GateDirection gateDir) => this.gateDir = gateDir;

    private float timer;

    private void Update()
    {

        timer += Time.deltaTime;
        if (timer > UIConstants.PLAYER_LOCK_DELAY) timer = UIConstants.PLAYER_LOCK_DELAY;
    }

    private static float Quadratic(float v, float accel, float max, float time)
    {
        if (v > max) return max * time;

        float v2 = v + accel * time;
        if (v2 > max)
        {
            float aTime = (v2 - v) / accel;
            return (v2 + v) * aTime / 2 + (time - aTime) * max;
        }
        else return (v + v2) * time / 2;
    }

    private static Vector3 Project(GateDirection gateDir, Vector2 origVel, float time)
    {
        return gateDir switch
        {
            GateDirection.Left => new(time * -Mathf.Max(Mathf.Abs(origVel.x), 8.3f), 0),
            GateDirection.Right => new(time * Mathf.Max(Mathf.Abs(origVel.x), 8.3f), 0),
            GateDirection.Top => new(0, 12 * time),
            GateDirection.Bot => new(0, -Quadratic(Mathf.Abs(origVel.y), 38, 20.1f, time)),
            GateDirection.Door => (Vector3)Vector2.zero,
            _ => throw new System.ArgumentException($"Unknown GateDir: {gateDir}"),
        };
    }

    private void LateUpdate()
    {
        rb2d!.simulated = false;
        knight!.position = origPos + Project(gateDir, origVel, timer);
    }

    private void FixedUpdate()
    {
        rb2d!.simulated = false;
        knight!.position = origPos + Project(gateDir, origVel, timer);
    }
}
