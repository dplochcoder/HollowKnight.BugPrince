using BugPrince.IC;
using BugPrince.IC.Locations;
using BugPrince.Scripts.InternalLib;
using BugPrince.Util;
using System.Collections.Generic;
using UnityEngine;

namespace BugPrince.Scripts.Framework;

[Shim]
internal class GemstoneBoulder : MonoBehaviour, IHitResponder
{
    private const float INVULN_DURATION = 0.15f;
    private const float SHAKE_DURATION = 0.3f;
    private const float NORMAL_SHAKE_RANGE = 0.2f;
    private const float SMALL_SHAKE_RANGE = 0.04f;

    [ShimField] public SpriteRenderer? Sprite;
    [ShimField] public List<AudioClip> HitClips = [];
    [ShimField] public AudioClip? GiveClip;
    [ShimField] public AudioClip? ShatterClip;

    private GemstoneBoulderPlacement? currentPlacement;
    private int currentItemIndex = -1;
    private int hp;

    private AudioSource? audioSource;
    private Vector3 origPos;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        origPos = Sprite!.transform.localPosition;

        ChooseNextItem();
        UpdateContainer();
    }

    public void Hit(HitInstance damageInstance)
    {
        if (currentPlacement == null) return;
        if (invulnTimer > 0) return;
        if (damageInstance.AttackType != AttackTypes.Nail) return;

        invulnTimer = INVULN_DURATION;
        shakeTimer = SHAKE_DURATION;

        var pd = PlayerData.instance;
        if (pd.GetInt(nameof(pd.nailSmithUpgrades)) < currentPlacement.Location.NailUpgradesThreshold)
        {
            shakeRange = SMALL_SHAKE_RANGE;
            audioSource?.PlayOneShot(SoundCache.failed_menu);
            return;
        }

        // TODO: Break crystals
        hp -= damageInstance.DamageDealt;
        shakeRange = NORMAL_SHAKE_RANGE;

        if (hp > 0)
        {
            audioSource?.PlayOneShot(HitClips.Random());
            return;
        }

        currentPlacement.Items[currentItemIndex].GiveOrFling(currentPlacement, transform);
        ChooseNextItem();
        UpdateContainer();
        audioSource?.PlayOneShot(currentPlacement != null ? GiveClip! : ShatterClip!);
    }

    private void ChooseNextItem()
    {
        foreach (var placement in GemstoneBoulderPlacement.ActivePlacements())
        {
            if (placement.AllObtained()) continue;
            if (currentPlacement != null && placement.Location.NailUpgradesThreshold < currentPlacement.Location.NailUpgradesThreshold) continue;

            for (int i = placement == currentPlacement ? currentItemIndex + 1 : 0; i < placement.Items.Count; i++)
            {
                if (placement.Items[i].IsObtained()) continue;

                // Found an unobtained item.
                currentPlacement = placement;
                currentItemIndex = i;
                return;
            }
        }

        // No item.
        currentPlacement = null;
        currentItemIndex = int.MaxValue;
    }

    private static EmbeddedSprite emptySprite = new("Game.empty");

    private void UpdateContainer()
    {
        Sprite!.sprite = currentPlacement?.Location?.BoulderSprite?.Value ?? emptySprite.Value;
        if (currentPlacement == null)
        {
            shakeTimer = 0;
            invulnTimer = 0;
            hp = -1;
            return;
        }

        hp = (5 + currentPlacement.Location.NailUpgradesThreshold * 4) * 10;
    }

    private float invulnTimer;
    private float shakeTimer;
    private float shakeRange;

    private void Update()
    {
        if (invulnTimer > 0)
        {
            invulnTimer -= Time.deltaTime;
            if (invulnTimer <= 0) invulnTimer = 0;
        }

        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0)
            {
                shakeTimer = 0;
                Sprite!.transform.localPosition = origPos;
            }
            else Sprite!.transform.localPosition = origPos + new Vector3(UnityEngine.Random.Range(-shakeRange, shakeRange), UnityEngine.Random.Range(-shakeRange, shakeRange));
        }
    }
}
