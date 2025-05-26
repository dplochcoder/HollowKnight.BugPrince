using BugPrince.IC;
using BugPrince.Util;
using PurenailCore.GOUtil;
using UnityEngine;

namespace BugPrince.UI;

internal class SceneChoice : MonoBehaviour
{
    private const float ASCEND_DISTANCE = 3;
    private const float ASCEND_TIME = 0.4f;
    private const float SHAKE_DURATION = 0.3f;
    private const float SHAKE_X_RANGE = 0.5f;
    private const float SHAKE_Y_RANGE = 0.5f;

    private const float COST_X_SPACE = 1f;
    private const float COST_Y = -1.5f;
    private static readonly Vector2 PIN_POS = new(-2f, 1.5f);

    internal static SceneChoice Create(SceneChoiceInfo info, Vector2 targetPos)
    {
        GameObject obj = new("SceneChoice");

        var choice = obj.AddComponent<SceneChoice>();

        var shaker = obj.AddShakerChild(SHAKE_X_RANGE, SHAKE_Y_RANGE, SHAKE_DURATION);
        var shakerObj = shaker.gameObject;
        var renderer = shakerObj.AddComponent<SpriteRenderer>();
        renderer.sprite = info.GetSceneSprite();
        renderer.SetUILayer(UISortingOrder.ScenePicture);
        shakerObj.FadeColor(Color.white.WithAlpha(0), Color.white, ASCEND_TIME);

        // TODO: Remove
        if (renderer.sprite.bounds.size.x < 1)
        {
            GameObject textObj = new("Text");
            textObj.transform.parent = shakerObj.transform;
            textObj.transform.localPosition = Vector3.zero;

            var text = textObj.AddComponent<TextMesh>();
            text.color = Color.white;
            text.alignment = TextAlignment.Center;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontSize = 24;
            text.text = info.Target.ToString();
        }

        GameObject pinIco = new("Pin");
        var pinRenderer = pinIco.AddComponent<SpriteRenderer>();
        pinRenderer.sprite = PushPinItem.sprite.Value;
        pinRenderer.SetUILayer(UISortingOrder.CostIcons);
        pinRenderer.transform.SetParent(shakerObj.transform);
        pinRenderer.transform.localPosition = PIN_POS;
        var pinTracker = pinIco.AddComponent<PinAnimator>();

        if (info.Cost.HasValue)
        {
            var (costType, cost) = info.Cost.Value;
            for (int i = 0; i < cost; i++)
            {
                GameObject costIco = new("Cost");
                costIco.transform.SetParent(shakerObj.transform);
                costIco.transform.localPosition = new(i * COST_X_SPACE - (cost - 1) * COST_X_SPACE / 2, COST_Y);

                var costRenderer = costIco.AddComponent<SpriteRenderer>();
                costRenderer.sprite = costType.GetSprite();
                costRenderer.SetUILayer(UISortingOrder.CostIcons);
                costIco.FadeColor(Color.white.WithAlpha(0), Color.white, ASCEND_TIME);
            }
        }

        choice.shaker = shaker;
        choice.pinAnimator = pinTracker;
        choice.targetPos = targetPos;
        return choice;
    }

    private Shaker? shaker;
    private PinAnimator? pinAnimator;
    private Vector2 targetPos;
    private float ascendTime;

    internal bool IsReady() => ascendTime >= ASCEND_TIME;

    internal void Shake() => shaker?.Shake();

    internal void Pin(bool show) => pinAnimator?.Pin(show);

    internal void FadeOut(float duration)
    {
        gameObject.FadeColor(Color.white, Color.white.WithAlpha(0), duration);
        foreach (var obj in gameObject.GetComponentsInChildren<SpriteRenderer>()) obj.gameObject.FadeColor(Color.white, Color.white.WithAlpha(0), duration);
    }

    private const float FLY_VELOCITY = 12;
    private bool flying = false;

    internal void FlyUp() => flying = true;

    private void Update()
    {
        if (flying) transform.Translate(new(0, FLY_VELOCITY * Time.deltaTime));
        if (ascendTime >= ASCEND_TIME) return;
        
        ascendTime += Time.deltaTime;
        if (ascendTime > ASCEND_TIME) ascendTime = ASCEND_TIME;

        transform.localPosition = new(targetPos.x, targetPos.y - ASCEND_DISTANCE * (1 - Mathf.Sin(Mathf.PI * (ascendTime / ASCEND_TIME) / 2)));
    }
}
