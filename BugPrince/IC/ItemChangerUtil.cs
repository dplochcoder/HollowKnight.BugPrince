using ItemChanger.Util;
using ItemChanger;
using UnityEngine;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.Locations;

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
        var clip = (shaman.LocateMyFSM("Conversation Control").GetState("Summon Anim").GetFirstActionOfType<AudioPlayerOneShotSingle>().audioClip.Value as AudioClip)!;
        var particles = Object.Instantiate(shaman.FindChild("Summon 1")!);

        particles.transform.position = pos;
        particles.AddComponent<ItemSummoner>().Init(self);

        particles.SetActive(true);
        particles.AddComponent<AudioSource>().PlayOneShot(clip);
    }
}

internal class ItemSummoner : MonoBehaviour
{
    private ContainerLocation? location;

    internal void Init(ContainerLocation location) => this.location = location;

    private ParticleSystem? particleSystem;

    private void Awake() => particleSystem = GetComponent<ParticleSystem>();

    private const float EASE_TIME = 2;
    private const float MIN_PARTICLES = 0;
    private const float MAX_PARTICLES = 40;

    private float delay = 0.5f;
    private float ease = EASE_TIME;
    private float post = 0.5f;

    private void Update()
    {
        var time = Time.deltaTime;
        if (delay > 0)
        {
            delay -= time;
            if (delay <= 0)
            {
                time -= delay;
                particleSystem!.emissionRate = 0;
                particleSystem.Play();
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
                particleSystem!.emissionRate = MIN_PARTICLES + (MAX_PARTICLES - MIN_PARTICLES) * (2f - ease);
                return;
            }
        }
        if (post > 0)
        {
            post -= time;
            if (post <= 0)
            {
                location!.GetContainer(out var obj, out var containerType);
                Container.GetContainer(containerType)!.ApplyTargetContext(obj, gameObject, 0);
                obj.SetActive(true);
            }
        }
    }
}