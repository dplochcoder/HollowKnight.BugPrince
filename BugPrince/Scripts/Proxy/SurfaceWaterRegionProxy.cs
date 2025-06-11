using BugPrince.Scripts.InternalLib;
using ItemChanger.Extensions;
using UnityEngine;

namespace BugPrince.Scripts.Proxy;

[Shim]
internal class SurfaceWaterRegionProxy : MonoBehaviour
{
    private void Awake()
    {
        var obj = Instantiate(BugPrincePreloader.Instance.SurfaceWaterRegion!);
        obj.transform.position = transform.position;

        var from = gameObject.GetComponent<BoxCollider2D>();
        var to = obj.GetComponent<BoxCollider2D>();
        to.offset = from.offset;
        to.size = from.size;

        to = obj.FindChild("Splash Surface")!.GetComponent<BoxCollider2D>();
        to.offset = from.offset;
        to.size = from.size;

        obj.SetActive(true);
        Destroy(gameObject);
    }
}
