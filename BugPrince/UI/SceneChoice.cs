using BugPrince.IC;
using BugPrince.Util;
using PurenailCore.GOUtil;
using UnityEngine;

namespace BugPrince.UI;

internal class SceneChoice : MonoBehaviour
{
    internal static SceneChoice Create(Transform parent, SceneChoiceInfo info, Vector2 targetPos)
    {
        GameObject obj = new("SceneChoice");
        obj.transform.SetParent(parent);

        var choice = obj.AddComponent<SceneChoice>();

        var shaker = obj.AddShakerChild(UIConstants.SCENE_SHAKE_X_RANGE, UIConstants.SCENE_SHAKE_Y_RANGE, UIConstants.SHAKE_TIME);
        var shakerObj = shaker.gameObject;
        var renderer = shakerObj.AddComponent<SpriteRenderer>();
        renderer.sprite = info.GetSceneSprite();
        renderer.SetUILayer(UISortingOrder.ScenePicture);
        shakerObj.FadeColor(Color.white.WithAlpha(0), Color.white, UIConstants.SCENE_ASCEND_TIME);

        // TODO: Remove
        if (renderer.sprite.bounds.size.x < 1)
        {
            GameObject textObj = new("Text");
            textObj.transform.SetParent(shakerObj.transform);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localScale = new(UIConstants.TEXT_SCALE, UIConstants.TEXT_SCALE, UIConstants.TEXT_SCALE);

            var text = textObj.AddComponent<TextMesh>();
            text.color = Color.white;
            text.alignment = TextAlignment.Center;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontSize = UIConstants.TEXT_SIZE;
            text.text = info.Target.ToString();

            text.GetComponent<MeshRenderer>().SetUILayer(UISortingOrder.SceneText);
        }

        GameObject pinIco = new("Pin");
        var pinRenderer = pinIco.AddComponent<SpriteRenderer>();
        pinRenderer.sprite = PushPinItem.sprite.Value;
        pinRenderer.SetUILayer(UISortingOrder.CostIcons);
        pinRenderer.transform.SetParent(shakerObj.transform);
        pinRenderer.transform.localPosition = UIConstants.PIN_POS;
        var pinTracker = pinIco.AddComponent<PinAnimator>();

        if (info.Cost.HasValue)
        {
            var (costType, cost) = info.Cost.Value;
            for (int i = 0; i < cost; i++)
            {
                GameObject costIco = new("Cost");
                costIco.transform.SetParent(shakerObj.transform);
                costIco.transform.localPosition = new(i * UIConstants.SCENE_COST_X_SPACE - (cost - 1) * UIConstants.SCENE_COST_X_SPACE / 2, UIConstants.SCENE_COST_Y);
                var scale = costType == Data.CostType.Coins ? UIConstants.SCENE_COST_COIN_SCALE : UIConstants.SCENE_COST_GEM_SCALE;
                costIco.transform.localScale = new(scale, scale, scale);

                var costRenderer = costIco.AddComponent<SpriteRenderer>();
                costRenderer.sprite = costType.GetSprite();
                costRenderer.SetUILayer(UISortingOrder.CostIcons);
                costIco.FadeColor(Color.white.WithAlpha(0), Color.white, UIConstants.SCENE_ASCEND_TIME);
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

    internal bool IsReady() => ascendTime >= UIConstants.SCENE_ASCEND_TIME;

    internal void Shake() => shaker?.Shake();

    internal void Pin(bool show) => pinAnimator?.Pin(show);

    internal void FadeOut(float duration) => gameObject.Recursively(go => go.FadeColor(Color.white.WithAlpha(0), duration));

    private const float FLY_VELOCITY = 28;
    private bool flying = false;

    internal void FlyUp() => flying = true;

    private void Update()
    {
        if (flying) transform.Translate(new(0, FLY_VELOCITY * Time.deltaTime));
        if (ascendTime >= UIConstants.SCENE_ASCEND_TIME) return;
        
        ascendTime += Time.deltaTime;
        if (ascendTime > UIConstants.SCENE_ASCEND_TIME) ascendTime = UIConstants.SCENE_ASCEND_TIME;

        transform.localPosition = new(targetPos.x, targetPos.y - UIConstants.SCENE_ASCEND_DISTANCE * (1 - Mathf.Sin(Mathf.PI * (ascendTime / UIConstants.SCENE_ASCEND_TIME) / 2)));
    }
}
