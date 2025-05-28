using BugPrince.IC;
using BugPrince.Util;
using PurenailCore.GOUtil;
using UnityEngine;

namespace BugPrince.UI;

internal class SelectionCorners : MonoBehaviour
{
    internal static SelectionCorners Create(GameObject parent, Vector2 pos)
    {
        var obj = parent.AddNewChild("SelectionCorners");
        obj.transform.localPosition = pos;

        var shaker = obj.AddNewChildShaker();

        SelectionCorner.Create(shaker.gameObject, new(-UIConstants.SELECTION_WIDTH / 2, UIConstants.SELECTION_HEIGHT / 2), 0);
        SelectionCorner.Create(shaker.gameObject, new(-UIConstants.SELECTION_WIDTH / 2, -UIConstants.SELECTION_HEIGHT / 2), 90);
        SelectionCorner.Create(shaker.gameObject, new(UIConstants.SELECTION_WIDTH / 2, -UIConstants.SELECTION_HEIGHT / 2), 180);
        SelectionCorner.Create(shaker.gameObject, new(UIConstants.SELECTION_WIDTH / 2, UIConstants.SELECTION_HEIGHT / 2), 270);

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

    internal void FadeOut(float duration) => gameObject.Recursively(go => go.FadeColor(Color.white.WithAlpha(0), duration));

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

    internal static void Create(GameObject parent, Vector2 pos, float rotation)
    {
        var localParent = parent.AddNewChild("CornerParent");
        localParent.transform.localPosition = pos;
        localParent.transform.localRotation = Quaternion.Euler(0, 0, rotation);

        var obj = localParent.AddNewChild("obj");
        obj.AddComponent<SelectionCorner>();

        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite.Value;
        renderer.SetUILayer(UISortingOrder.SelectionCorners);
        obj.FadeColor(new(1, 1, 1, 0), Color.white, UIConstants.SELECTION_FADE_IN_TIME);
    }

    private float timer;

    private void Update()
    {
        timer = (timer + Time.deltaTime) % UIConstants.SELECTION_OSCILLATION_TIME;
        var sine = Mathf.Sin(timer * 2 * Mathf.PI / UIConstants.SELECTION_OSCILLATION_TIME);
        transform.localPosition = new(UIConstants.SELECTION_OSCILLATION_DIST * sine, -UIConstants.SELECTION_OSCILLATION_DIST * sine);
    }
}