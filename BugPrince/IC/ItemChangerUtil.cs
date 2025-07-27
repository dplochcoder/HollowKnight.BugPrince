using ItemChanger.Util;
using ItemChanger;
using UnityEngine;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.Locations;
using BugPrince.Util;

namespace BugPrince.IC;

enum FlingDirection
{
    Any,
    Left,
    Right,
    Down
}

internal static class ItemChangerUtil
{
    internal static bool GiveOrFling(this AbstractItem self, AbstractPlacement placement, Transform src, FlingDirection dir = FlingDirection.Any)
    {
        if (self.IsObtained()) return false;

        GiveInfo info = new()
        {
            Container = Container.Chest,
            FlingType = FlingType.Everywhere,
            Transform = src,
            MessageType = MessageType.Corner
        };

        if (self.GiveEarly(Container.Chest)) self.Give(placement, info);
        else
        {
            var shiny = ShinyUtility.MakeNewShiny(placement, self, FlingType.Everywhere);
            shiny.transform.position = src.position;
            shiny.SetActive(true);

            var fsm = shiny.LocateMyFSM("Shiny Control");
            switch (dir)
            {
                case FlingDirection.Left: ShinyUtility.FlingShinyLeft(fsm); break;
                case FlingDirection.Right: ShinyUtility.FlingShinyRight(fsm); break;
                case FlingDirection.Down: ShinyUtility.FlingShinyDown(fsm); break;
                default: ShinyUtility.FlingShinyRandomly(fsm); break;
            }
        }
        return true;
    }

    internal static void SummonItems(this ContainerLocation self, Vector2 pos)
    {
        var shaman = BugPrincePreloader.Instance.ShamanMeeting!;
        var fsm = shaman.LocateMyFSM("Conversation Control");
        var summonClip = (fsm.GetState("Summon Anim").GetFirstActionOfType<AudioPlayerOneShotSingle>().audioClip.Value as AudioClip)!;
        var finalClip = (fsm.GetState("Spell Appear").GetFirstActionOfType<AudioPlayerOneShotSingle>().audioClip.Value as AudioClip)!;
        var particles = Object.Instantiate(shaman.FindChild("Summon 1")!);

        particles.transform.position = pos;
        particles.transform.localScale *= 1.1f;
        particles.AddComponent<ItemSummoner>().Init(self, finalClip);

        particles.SetActive(true);
        particles.PlaySoundEffect(summonClip);
    }
}

internal class ItemSummoner : MonoBehaviour
{
    private ContainerLocation? location;
    private AudioClip? finalClip;

    internal void Init(ContainerLocation location, AudioClip finalClip)
    {
        this.location = location;
        this.finalClip = finalClip;
    }

    private ParticleSystem? particleSystem;

    private void Awake() => particleSystem = GetComponent<ParticleSystem>();

    private const float EASE_TIME = 2;
    private const float MIN_PARTICLES = 0;
    private const float MAX_PARTICLES = 40;

    private float delay = 0.5f;
    private float ease = EASE_TIME;
    private float post = 0.5f;

    private GameObject? shinyMarker;

    private void Update()
    {
        var time = Time.deltaTime;
        if (delay > 0)
        {
            delay -= time;
            if (delay <= 0)
            {
                time -= delay;
                var emission = particleSystem!.emission;
                emission.rateOverTime = 0;
                emission.rateOverDistance = 0;
                particleSystem.Play();

                shinyMarker = GameObjectUtil.MakeShinyDecorator();
                shinyMarker.transform.position = gameObject.transform.position;
                shinyMarker.transform.localScale = Vector3.zero;
                shinyMarker.SetActive(true);
            }
            else return;
        }
        if (ease > 0)
        {
            ease -= time;
            if (ease <= 0)
            {
                time -= ease;
                particleSystem!.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            else
            {
                var emission = particleSystem!.emission;
                emission.rateOverTime = MIN_PARTICLES + (MAX_PARTICLES - MIN_PARTICLES) * (EASE_TIME - ease);

                var pct = (EASE_TIME - ease) / EASE_TIME;
                var scale = (1 - Mathf.Sin((pct + 1) * Mathf.PI / 2)) * 0.75f;
                shinyMarker!.transform.localScale = new(scale, scale, scale);
                return;
            }
        }
        if (post > 0)
        {
            post -= time;
            if (post <= 0)
            {
                shinyMarker?.SetActive(false);
                gameObject.PlaySoundEffect(finalClip);

                location!.GetContainer(out var obj, out var containerType);
                Container.GetContainer(containerType)!.ApplyTargetContext(obj, gameObject, 0);
                obj.SetActive(true);
            }
        }
    }
}