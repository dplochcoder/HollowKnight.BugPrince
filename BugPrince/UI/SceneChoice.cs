using BugPrince.IC;
using BugPrince.IC.Items;
using BugPrince.Util;
using PurenailCore.GOUtil;
using UnityEngine;

namespace BugPrince.UI;

internal class SceneChoice : MonoBehaviour
{
    internal static SceneChoice Create(TransitionSelectionModule module, GameObject parent, SceneChoiceInfo info, Vector2 targetPos)
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
        var pinAnimator = pinIco.AddComponent<PinAnimator>();
        pinAnimator.Pin(info.Pinned);

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
                costRenderer.sprite = costType switch { Data.CostType.Coins => CoinItem.LargeSprite.Value, Data.CostType.Gems => GemItem.LargeSprite.Value, _ => throw costType.InvalidEnum() };
                costRenderer.SetUILayer(UISortingOrder.CostIcons);
                costIco.FadeColor(Color.white.WithAlpha(0), Color.white, UIConstants.SCENE_ASCEND_TIME);
            }
        }

        choice.pinShaker = pinShaker;
        choice.costsShaker = costsShaker;
        choice.pinAnimator = pinAnimator;
        choice.targetPos = targetPos;
        if (!module.MatchingTransitions()) choice.particleFactory = new(obj.transform, info.Target.GetDirection());
            
        return choice;
    }

    private Shaker? pinShaker;
    private Shaker? costsShaker;
    private PinAnimator? pinAnimator;
    private Vector2 targetPos;

    private GateDirectionParticleFactory? particleFactory;

    private float ascendTime;

    internal bool IsReady() => ascendTime >= UIConstants.SCENE_ASCEND_TIME;

    internal void SetPinned(bool value) => pinAnimator?.Pin(value);

    internal void ShakePin() => pinShaker?.Shake();

    internal void ShakeCosts() => costsShaker?.Shake();

    internal void FadeOut(float duration)
    {
        particleFactory = null;
        gameObject.Recursively(go => go.FadeColor(Color.white.WithAlpha(0), duration));
    }

    private const float FLY_VELOCITY = 28;
    private bool flying = false;

    internal void FlyUp() => flying = true;

    private void Update()
    {
        particleFactory?.Update(Time.deltaTime);

        if (flying) transform.Translate(new(0, FLY_VELOCITY * Time.deltaTime));
        if (ascendTime >= UIConstants.SCENE_ASCEND_TIME) return;
        
        ascendTime += Time.deltaTime;
        if (ascendTime > UIConstants.SCENE_ASCEND_TIME) ascendTime = UIConstants.SCENE_ASCEND_TIME;

        transform.localPosition = new(targetPos.x, targetPos.y - UIConstants.SCENE_ASCEND_DISTANCE * (1 - Mathf.Sin(Mathf.PI * (ascendTime / UIConstants.SCENE_ASCEND_TIME) / 2)));
    }
}

internal class GateDirectionParticleFactory : AbstractParticleFactory<GateDirectionParticleFactory, GateDirectionParticle>
{
    private static readonly EmbeddedSprite sprite = new("UI.fullwhite");
    private const float FULL_EMISSION_RATE = 250;
    private const float LIFETIME = 1;

    private static float EmissionRate(GateDirection dir) => FULL_EMISSION_RATE * dir switch
    {
        GateDirection.Left or GateDirection.Right => UIConstants.SELECTION_WIDTH / (2 * (UIConstants.SELECTION_WIDTH + UIConstants.SELECTION_HEIGHT)),
        GateDirection.Top or  GateDirection.Bot => UIConstants.SELECTION_HEIGHT / (2 * (UIConstants.SELECTION_WIDTH + UIConstants.SELECTION_HEIGHT)),
        GateDirection.Door => 1,
        _ => throw dir.InvalidEnum()
    };

    private readonly Transform parent;
    private readonly GateDirection dir;
    private readonly RandomFloatTicker ticker;

    internal GateDirectionParticleFactory(Transform parent, GateDirection dir)
    {
        this.parent = parent;
        this.dir = dir;
        ticker = new(1 / EmissionRate(dir), 1 / EmissionRate(dir));
    }

    protected override string GetObjectName() => "GateDirectionParticle";

    protected override Sprite GetSprite() => sprite.Value;

    protected override (GameObject, SpriteRenderer) CreateSprite()
    {
        var (obj, sprite) = base.CreateSprite();
        sprite.SetUILayer(UISortingOrder.SceneParticles);
        return (obj, sprite);
    }

    internal void Update(float time)
    {
        foreach (var elapsed in ticker.Tick(time))
        {
            if (!Launch(elapsed, LIFETIME, out var particle)) continue;

            particle.Init(RandomPos());
            particle.Finalize(elapsed);
        }
    }

    private Vector3 TopLeft() => new(parent.position.x - UIConstants.SELECTION_WIDTH / 2, parent.position.y + UIConstants.SELECTION_HEIGHT / 2);
    private Vector3 TopRight() => new(parent.position.x + UIConstants.SELECTION_WIDTH / 2, parent.position.y + UIConstants.SELECTION_HEIGHT / 2);
    private Vector3 BottomLeft() => new(parent.position.x - UIConstants.SELECTION_WIDTH / 2, parent.position.y - UIConstants.SELECTION_HEIGHT / 2);
    private Vector3 BottomRight() => new(parent.position.x + UIConstants.SELECTION_WIDTH / 2, parent.position.y - UIConstants.SELECTION_HEIGHT / 2);

    private static readonly GateDirection[] DIRS = [GateDirection.Top, GateDirection.Bot, GateDirection.Left, GateDirection.Right];

    private Vector3 RandomPos() => dir switch
    {
        GateDirection.Door => RandomPos(DIRS.Random()),
        _ => RandomPos(dir)
    };

    private Vector3 RandomPos(GateDirection d) => d switch
    {
        GateDirection.Top => Vector3.Lerp(TopLeft(), TopRight(), Random.Range(0f, 1f)),
        GateDirection.Left => Vector3.Lerp(TopLeft(), BottomLeft(), Random.Range(0f, 1f)),
        GateDirection.Right => Vector3.Lerp(TopRight(), BottomRight(), Random.Range(0f, 1f)),
        GateDirection.Bot => Vector3.Lerp(BottomLeft(), BottomRight(), Random.Range(0f, 1f)),
        _ => throw d.InvalidEnum()
    };
}

internal class GateDirectionParticle : AbstractParticle<GateDirectionParticleFactory, GateDirectionParticle>
{
    private Vector3 srcPos;
    private Vector3 direction;

    private const float BASE_SCALE = 0.25f;
    private const float MIN_DIST = 0.2f;
    private const float MAX_DIST = 0.35f;

    internal void Init(Vector3 srcPos)
    {
        this.srcPos = srcPos;

        var angle = Random.Range(0f, 360f);
        var dist = Random.Range(MIN_DIST, MAX_DIST);
        direction = Quaternion.Euler(0, 0, angle) * Vector3.right * dist;
    }

    protected override float GetAlpha() => Mathf.Sin(Progress * Mathf.PI);

    protected override Vector3 GetPos() => srcPos + direction * Progress;

    protected override Vector3 GetScale()
    {
        var scale = Mathf.Sin(Progress * Mathf.PI) * BASE_SCALE;
        return new(scale, scale, scale);
    }

    protected override GateDirectionParticle Self() => this;
}
