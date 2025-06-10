using BugPrince.Scripts.InternalLib;
using UnityEngine;

namespace BugPrince.Scripts.Framework;

[Shim]
internal class ShadeMarker : MonoBehaviour
{
    private void Awake()
    {
        var obj = Instantiate(BugPrincePreloader.Instance.ShadeMarker!, transform.position, Quaternion.identity);
        obj.SetActive(true);
    }
}