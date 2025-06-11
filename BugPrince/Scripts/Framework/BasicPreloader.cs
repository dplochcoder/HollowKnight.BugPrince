using BugPrince.Scripts.InternalLib;
using UnityEngine;

namespace BugPrince.Scripts.Framework;

internal abstract class BasicPreloader : MonoBehaviour
{
    protected abstract GameObject GetTemplate(BugPrincePreloader preloader);

    private void Awake()
    {
        GameObject obj = Instantiate(GetTemplate(BugPrincePreloader.Instance));
        obj.transform.position = transform.position;
        obj.transform.rotation = transform.rotation;
        obj.transform.localScale = transform.localScale;
        obj.SetActive(true);

        Destroy(gameObject);
    }
}

[Shim]
internal class CrystalGlobe : BasicPreloader
{
    protected override GameObject GetTemplate(BugPrincePreloader preloader) => preloader.CrystalGlobe!;
}

[Shim]
internal class RuinsWater : BasicPreloader
{
    protected override GameObject GetTemplate(BugPrincePreloader preloader) => preloader.RuinsWater!;
}
