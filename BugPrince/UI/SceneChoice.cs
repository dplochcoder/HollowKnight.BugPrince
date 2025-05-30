using BugPrince.IC.Items;
using BugPrince.Util;
using PurenailCore.GOUtil;
using UnityEngine;

namespace BugPrince.UI;

internal class SceneChoice : MonoBehaviour
{
    internal static SceneChoice Create(GameObject parent, SceneChoiceInfo info, Vector2 targetPos)
    {
        var obj = parent.AddNewChild("SceneChoice");
        var choice = obj.AddComponent<SceneChoice>();

        var costsShaker = obj.AddNewChildShaker(UIConstants.SCENE_COST_SHAKE_SPAN);
        var pinShaker = obj.AddNewChildShaker();

        var sceneSprite = obj.AddNewChild("Scene");
        sceneSprite.transform.SetParent(obj.transform);
        sceneSprite.transform.localScale = new(UIConstants.SCENE_SCALE, UIConstants.SCENE_SCALE, UIConstants.SCENE_SCALE);
        var renderer = sceneSprite.AddComponent<SpriteRenderer>();
        renderer.sprite = info.GetSceneSprite();
        renderer.SetUILayer(UISortingOrder.ScenePicture);
        sceneSprite.FadeColor(Color.white.WithAlpha(0), Color.white, UIConstants.SCENE_ASCEND_TIME);

        if (renderer.sprite.bounds.size.x < 1)
        {
            var textObj = obj.AddNewChild("Text");
            textObj.transform.localScale = new(UIConstants.TEXT_SCALE, UIConstants.TEXT_SCALE, UIConstants.TEXT_SCALE);

            var text = textObj.AddComponent<TextMesh>();
            text.color = Color.white;
            text.alignment = TextAlignment.Center;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontSize = UIConstants.TEXT_SIZE;
            text.text = info.Target.SceneName;

            text.GetComponent<MeshRenderer>().SetUILayer(UISortingOrder.SceneText);
        }

        var pinIco = pinShaker.gameObject.AddNewChild("Pin");
        pinIco.transform.localPosition = UIConstants.PIN_POS;
        var pinRenderer = pinIco.AddComponent<SpriteRenderer>();
        pinRenderer.sprite = PushPinItem.LargeSprite.Value;
        pinRenderer.SetUILayer(UISortingOrder.CostIcons);
        var pinTracker = pinIco.AddComponent<PinAnimator>();

        if (info.Cost.HasValue)
        {
            var (costType, cost) = info.Cost.Value;
            for (int i = 0; i < cost; i++)
            {
                var costIco = costsShaker.gameObject.AddNewChild("Cost");
                costIco.transform.localPosition = new(i * UIConstants.SCENE_COST_X_SPACE - (cost - 1) * UIConstants.SCENE_COST_X_SPACE / 2, UIConstants.SCENE_COST_Y);
                var scale = costType == Data.CostType.Coins ? UIConstants.SCENE_COST_COIN_SCALE : UIConstants.SCENE_COST_GEM_SCALE;
                costIco.transform.localScale = new(scale, scale, scale);

                var costRenderer = costIco.AddComponent<SpriteRenderer>();
                costRenderer.sprite = costType.GetSprite();
                costRenderer.SetUILayer(UISortingOrder.CostIcons);
                costIco.FadeColor(Color.white.WithAlpha(0), Color.white, UIConstants.SCENE_ASCEND_TIME);
            }
        }

        choice.pinShaker = pinShaker;
        choice.costsShaker = costsShaker;
        choice.pinAnimator = pinTracker;
        choice.targetPos = targetPos;
        return choice;
    }

    private Shaker? pinShaker;
    private Shaker? costsShaker;
    private PinAnimator? pinAnimator;
    private Vector2 targetPos;
    private float ascendTime;

    internal bool IsReady() => ascendTime >= UIConstants.SCENE_ASCEND_TIME;

    internal void SetPinned(bool value) => pinAnimator?.Pin(value);

    internal void ShakePin() => pinShaker?.Shake();

    internal void ShakeCosts() => costsShaker?.Shake();

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
