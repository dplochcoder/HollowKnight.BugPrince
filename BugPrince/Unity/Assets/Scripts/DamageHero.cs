using UnityEngine;

public class DamageHero : MonoBehaviour
{
    public int damageDealt = 1;

    [Header("1=NON_HAZARD, 2=SPIKES, 3=ACID, 4=LAVA, 5=PIT")]
    public int hazardType = 1;

    public bool shadowDashHazard;

    public bool resetOnEnable;
}
