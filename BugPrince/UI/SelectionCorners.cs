using BugPrince.IC;
using BugPrince.Util;
using UnityEngine;

namespace BugPrince.UI;

internal class SelectionCorners : MonoBehaviour
{
    private const float WIDTH = 4;
    private const float HEIGHT = 2.5f;
    private const float SHAKE_TIME = 0.3f;
    private const float SHAKE_SPAN = 0.2f;
    private const float VELOCITY = 30;

    internal static SelectionCorners Create(Vector2 pos)
    {
        GameObject obj = new("SelectionCorners");
        obj.transform.position = pos;

        var shaker = obj.AddShakerChild(SHAKE_SPAN, SHAKE_SPAN, SHAKE_TIME);

        SelectionCorner.Create(shaker.gameObject, new(-WIDTH / 2, HEIGHT / 2), 0);
        SelectionCorner.Create(shaker.gameObject, new(-WIDTH / 2, -HEIGHT / 2), 90);
        SelectionCorner.Create(shaker.gameObject, new(WIDTH / 2, -HEIGHT / 2), 180);
        SelectionCorner.Create(shaker.gameObject, new(WIDTH / 2, HEIGHT / 2), 270);

        var corners = obj.AddComponent<SelectionCorners>();
        corners.shaker = shaker;
        corners.target = pos;

        obj.SetActive(true);
        return corners;
    }

    private Shaker? shaker;
    private Vector3 target;

    internal void UpdateTarget(Vector2 target) => this.target = target;

    internal void Shake() => shaker?.Shake();

    private void Update()
    {
        var dist = target - transform.localPosition;
        var move = Time.deltaTime * VELOCITY;

        if (move >= dist.magnitude) transform.localPosition = target;
        else transform.Translate(move * dist.normalized);
    }
}

internal class SelectionCorner : MonoBehaviour
{
    private static EmbeddedSprite sprite = new("UI.corner");

    private const float OSCILLATION_DIST = 0.25f;
    private const float OSCILLATION_TIME = 2.5f;

    private const float FADE_IN_TIME = 0.5f;

    internal static void Create(GameObject parent, Vector2 pos, float rotation)
    {
        GameObject obj = new("Corner");

        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite.Value;
        obj.FadeColor(new(1, 1, 1, 0), Color.white, FADE_IN_TIME);

        GameObject localParent = new("CornerParent");
        obj.transform.SetParent(localParent.transform);
        obj.transform.localPosition = Vector3.zero;

        localParent.transform.SetParent(parent.transform);
        localParent.transform.localPosition = pos;
        localParent.transform.localRotation = Quaternion.Euler(0, 0, rotation);

        localParent.SetActive(true);
        obj.SetActive(true);
    }

    private float timer;

    private void Update()
    {
        timer = (timer + Time.deltaTime) % OSCILLATION_TIME;
        var sine = Mathf.Sin(timer * Mathf.PI / (2 * OSCILLATION_TIME));
        transform.localPosition = new(OSCILLATION_DIST * sine, -OSCILLATION_DIST * sine);
    }
}