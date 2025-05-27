using BugPrince.IC;
using BugPrince.Util;
using UnityEngine;

namespace BugPrince.UI;

internal class SelectionCorners : MonoBehaviour
{
    internal static SelectionCorners Create(Transform parent, Vector2 pos)
    {
        GameObject obj = new("SelectionCorners");
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;

        var shaker = obj.AddShakerChild(UIConstants.SELECTION_SHAKE_SPAN, UIConstants.SELECTION_SHAKE_SPAN, UIConstants.SHAKE_TIME);

        SelectionCorner.Create(shaker.transform, new(-UIConstants.SELECTION_WIDTH / 2, UIConstants.SELECTION_HEIGHT / 2), 0);
        SelectionCorner.Create(shaker.transform, new(-UIConstants.SELECTION_WIDTH / 2, -UIConstants.SELECTION_HEIGHT / 2), 90);
        SelectionCorner.Create(shaker.transform, new(UIConstants.SELECTION_WIDTH / 2, -UIConstants.SELECTION_HEIGHT / 2), 180);
        SelectionCorner.Create(shaker.transform, new(UIConstants.SELECTION_WIDTH / 2, UIConstants.SELECTION_HEIGHT / 2), 270);

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
        var move = Time.deltaTime * UIConstants.SELECTION_VELOCITY;

        if (move >= dist.magnitude) transform.localPosition = target;
        else transform.Translate(move * dist.normalized);
    }
}

internal class SelectionCorner : MonoBehaviour
{
    private static readonly EmbeddedSprite sprite = new("UI.corner");

    internal static void Create(Transform parent, Vector2 pos, float rotation)
    {
        GameObject obj = new("Corner");
        obj.AddComponent<SelectionCorner>();

        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite.Value;
        renderer.SetUILayer(UISortingOrder.SelectionCorners);
        obj.FadeColor(new(1, 1, 1, 0), Color.white, UIConstants.SELECTION_FADE_IN_TIME);

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
        timer = (timer + Time.deltaTime) % UIConstants.SELECTION_OSCILLATION_TIME;
        var sine = Mathf.Sin(timer * 2 * Mathf.PI / UIConstants.SELECTION_OSCILLATION_TIME);
        transform.localPosition = new(UIConstants.SELECTION_OSCILLATION_DIST * sine, -UIConstants.SELECTION_OSCILLATION_DIST * sine);
    }
}