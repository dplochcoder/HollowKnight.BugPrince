using System.Collections.Generic;
using UnityEngine;

namespace BugPrince.Scripts.Lib
{
    public static class GameObjectsExt
    {
        public static IEnumerable<GameObject> Children(this GameObject self)
        {
            foreach (Transform transform in self.transform) yield return transform.gameObject;
        }
    }
}
