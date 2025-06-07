using BugPrince.Scripts.InternalLib;
using UnityEngine;

namespace BugPrince.Scripts.Framework;

[Shim]
class PatchPlayMakerManager : MonoBehaviour
{
    [ShimField] public Transform? ManagerTransform;

    public void Awake()
    {
        var obj = Instantiate(BugPrincePreloader.Instance.PlayMaker!, ManagerTransform!);
        obj.SetActive(true);
        obj.name = "PlayMaker Unity 2D";
    }
}
