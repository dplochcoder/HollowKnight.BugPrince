using UnityEngine;
using SFCore.MonoBehaviours;
using System.Collections.Generic;
using BugPrince.Scripts.Proxy;

namespace BugPrince.Scripts.Lib
{
    public class OptimizerFinder
    {
        private enum FixResult
        {
            UNCHANGED,
            CHANGED,
            DELETED,
        }

        private static FixResult ChangedResult(bool changed) => changed ? FixResult.CHANGED : FixResult.UNCHANGED;

        private static bool UpdateFloat(ref float src, float dest)
        {
            if (Mathf.Abs(src - dest) < 0.001f) return false;

            src = dest;
            return true;
        }

        private static bool OptimizeObject(Object o, bool optimized)
        {
            if (optimized) UnityEditorShims.MarkDirty(o);
            return optimized;
        }

        private static bool FixAll<T>(System.Func<T, FixResult> fixer) where T : Component
        {
            bool changed = false;
            foreach (var t in Object.FindObjectsOfType<T>(true))
            {
                var result = fixer(t);
                changed |= result != FixResult.UNCHANGED;
                if (result == FixResult.CHANGED) UnityEditorShims.MarkDirty(t);
            }
            return changed;
        }

        public static List<string> FixScene()
        {
            var updates = new List<string>();
            void Update(string name, bool fnResult)
            {
                if (fnResult) updates.Add(name);
            };

            Update("AddRequiredObjects()", AddRequiredObjects());
            Update("FixScenery()", FixScenery());
            Update("FixTiles()", FixTiles());
            Update("FixAll<BlurPlanePatcher>(FixBPP)", FixAll<BlurPlanePatcher>(FixBPP));
            Update("FixAll<CameraLockAreaProxy>(FixCLAP)", FixAll<CameraLockAreaProxy>(FixCLAP));
            Update("FixAll<HeroDetectorProxy>(FixHDP)", FixAll<HeroDetectorProxy>(FixHDP));
            Update("FixAll<HazardRespawnTrigger>(FixHRT)", FixAll<HazardRespawnTrigger>(FixHRT));
            Update("FixAll<SceneDataOptimizer>(sdo => ChangedResult(sdo.OptimizeScene()))", FixAll<SceneDataOptimizer>(sdo => ChangedResult(sdo.OptimizeScene())));
            Update("FixAll<TransitionPoint>(FixTP)", FixAll<TransitionPoint>(FixTP));
            return updates;
        }

        private static bool AddRequiredObjects()
        {
            var objects = new string[] { "_Transition Gates", "Darkness", "CameraLocks", "HRTs", "Secrets" };

            bool changed = false;
            foreach (var name in objects)
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    go = new GameObject(name);
                    UnityEditorShims.MarkDirty(go);
                    changed = true;
                }
            }
            return changed;
        }

        private static Dictionary<string, int> SortingLayers = new Dictionary<string, int>()
        {
            {"Far BG 2", -6},
            {"Far BG 1", -5},
            {"Mid BG", -4},
            {"Immediate BG", -3},
            {"Actors", -2},
            {"Player", -1},
            {"Default", 0},
            {"Tiles", 1},
            {"MID Dressing", 2},
            {"Immediate FG", 3},
            {"Far FG", 4},
            {"Vignette", 5},
            {"Over", 6},
            {"HUD", 7},
        };

        private static bool FixTiles()
        {
            List<string> names = new List<string>();
            names.Add("reflection_");
            names.Add("snow_");
            names.Add("templeA_");
            names.Add("tower_");

            bool changed = false;
            foreach (var spriteRenderer in GameObject.Find("_Scenery").GetComponentsInChildren<SpriteRenderer>())
            {
                var go = spriteRenderer.gameObject;
                foreach (var prefix in names)
                {
                    if (spriteRenderer.name.StartsWith(prefix))
                    {
                        foreach (var comp in go.GetComponents<SpritePatcher>())
                        {
                            Object.DestroyImmediate(comp);
                            changed = true;
                        }

                        if (spriteRenderer.sortingLayerName != "Far FG")
                        {
                            spriteRenderer.sortingLayerName = "Far FG";
                            changed = true;
                        }
                    }
                }
            }

            return changed;
        }

        private static bool FixScenery()
        {
            var go = GameObject.Find("_Scenery");
            if (go == null)
            {
                go = new GameObject("_Scenery");
                go.AddComponent<SpritePatcher>();
                return true;
            }

            bool changed = false;

            if (Mathf.Sqrt(2) > 2)  // No depth fixing for tiles
            {
                Dictionary<(int, int, int), List<SpriteRenderer>> depthBuckets = new Dictionary<(int, int, int), List<SpriteRenderer>>();
                foreach (var spriteRenderer in go.GetComponentsInChildren<SpriteRenderer>())
                {
                    var quat = spriteRenderer.gameObject.transform.localRotation;
                    var ea = quat.eulerAngles;
                    if (ea.x != 0 || ea.y != 0)
                    {
                        ea.x = 0;
                        ea.y = 0;
                        spriteRenderer.gameObject.transform.localRotation = Quaternion.Euler(ea);
                        changed = true;
                    }

                    var z = (int)System.Math.Round(spriteRenderer.gameObject.transform.position.z * 100);
                    int layer = SortingLayers[spriteRenderer.sortingLayerName];
                    if (z == 0) z = System.Math.Sign(layer - 3);

                    var depthBucket = (-z, layer, spriteRenderer.sortingOrder);
                    depthBuckets.GetOrAddNew(depthBucket).Add(spriteRenderer);
                }

                // Sort sprites by all parameters, then group by z.
                List<((int, int, int), List<SpriteRenderer>)> sorted = new List<((int, int, int), List<SpriteRenderer>)>();
                depthBuckets.ForEach(pair => sorted.Add((pair.Key, pair.Value)));
                sorted.SortBy(pair => pair.Item1);

                Dictionary<int, List<List<SpriteRenderer>>> zBuckets = new Dictionary<int, List<List<SpriteRenderer>>>();
                foreach (var (k, v) in sorted) zBuckets.GetOrAddNew(k.Item1).Add(v);
                List<(int, List<List<SpriteRenderer>>)> sortedZBuckets = new List<(int, List<List<SpriteRenderer>>)>();
                zBuckets.ForEach(pair => sortedZBuckets.Add((pair.Key, pair.Value)));
                sortedZBuckets.SortBy(pair => pair.Item1);

                int curMax = int.MaxValue;
                foreach (var (z, groups) in sortedZBuckets)
                {
                    curMax = System.Math.Min(curMax, groups.Count / 2 - z);

                    foreach (var group in groups)
                    {
                        var newZ = curMax / 100.0f;

                        string layerName = "Default";
                        if (newZ == 0) layerName = "Immediate FG";
                        if (newZ < 0) layerName = "Far FG";

                        foreach (var spriteRenderer in group)
                        {
                            var pos = spriteRenderer.gameObject.transform.position;
                            pos.z = newZ;
                            spriteRenderer.gameObject.transform.position = pos;

                            if (spriteRenderer.sortingLayerName != layerName)
                            {
                                spriteRenderer.sortingLayerName = layerName;
                                changed = true;
                            }

                            if (spriteRenderer.sortingOrder != 0)
                            {
                                spriteRenderer.sortingOrder = 0;
                                changed = true;
                            }
                        }

                        --curMax;
                    }
                }
            }


            if (go.GetComponent<SpritePatcher>() == null)
            {
                go.AddComponent<SpritePatcher>();
                changed = true;
            }

            return changed;
        }

        private static FixResult FixBPP(BlurPlanePatcher bpp)
        {
            var stageSize = GameObject.Find("TilemapGrid/Tilemap").GetComponent<UnityEngine.Tilemaps.Tilemap>().size;
            var z = bpp.gameObject.transform.position.z;
            bool changed = MathExt.UpdateLocalRotation(bpp.gameObject.transform, Quaternion.Euler(270, 0, 0));
            changed |= MathExt.UpdatePosition(bpp.gameObject.transform, new Vector3(stageSize.x / 2, stageSize.y / 2, z));
            changed |= MathExt.UpdateLocalScale(bpp.gameObject.transform, new Vector3((stageSize.x / 10) + z, 1, (stageSize.y / 10) + z));
            return ChangedResult(changed);
        }

        private static FixResult FixCLAP(CameraLockAreaProxy clap)
        {
            bool changed = false;
            if (MathExt.NeedsSnap(clap.gameObject.transform.position, 0.5f))
            {
                clap.gameObject.transform.position = MathExt.Snap(clap.gameObject.transform.position, 0.5f);
                changed = true;
            }

            foreach (Transform child in clap.gameObject.transform.parent.gameObject.transform)
            {
                var b2d = child.gameObject.GetComponent<BoxCollider2D>();
                if (MathExt.Snap(b2d, 1f))
                {
                    changed = true;
                    UnityEditorShims.MarkDirty(b2d.gameObject);
                }
            }

            return ChangedResult(changed);
        }

        private static FixResult FixHDP(HeroDetectorProxy hdp)
        {
            bool changed = false;

            if (hdp.gameObject.layer != 13)
            {
                changed = true;
                hdp.gameObject.layer = 13;
            }

            foreach (var collider in hdp.gameObject.GetComponents<Collider2D>())
            {
                if (!collider.isTrigger)
                {
                    collider.isTrigger = true;
                    changed = true;
                }
            }

            return ChangedResult(changed);
        }

        private static FixResult FixHRT(HazardRespawnTrigger hrt)
        {
            bool changed = false;
            var bc = hrt.gameObject.GetComponent<BoxCollider2D>();
            if (bc != null)
            {
                changed |= !bc.isTrigger;
                bc.isTrigger = true;
            }

            if (hrt.respawnMarker == null)
            {
                bool isFixed = false;
                foreach (Transform child in hrt.gameObject.transform)
                {
                    var hrm = child.gameObject.GetComponent<HazardRespawnMarker>();
                    if (hrm != null)
                    {
                        hrt.respawnMarker = hrm;
                        isFixed = true;
                        changed = true;
                        break;
                    }
                }

                if (!isFixed) Debug.LogError($"{hrt.name} is missing its HazardRespawnMarker");
            }

            return ChangedResult(changed);
        }

        //private static FixResult FixSM(SecretMask sm)
        //{
        //bool changed = false;
        //foreach (var spriteRenderer in sm.GetComponentsInChildren<SpriteRenderer>())
        //{
        //var pos = spriteRenderer.gameObject.transform.position;
        //if (spriteRenderer.sortingLayerName != "Far FG" || spriteRenderer.sortingOrder != 10 || pos.z != -1)
        //{
        //spriteRenderer.sortingLayerName = "Far FG";
        //spriteRenderer.sortingOrder = 10;
        //spriteRenderer.gameObject.transform.position = new Vector3(pos.x, pos.y, -1);
        //changed = true;
        //}
        //}
        //return ChangedResult(changed);
        //}

        private static FixResult FixTP(TransitionPoint tp)
        {
            bool changed = false;
            var bc = tp.gameObject.GetComponent<BoxCollider2D>();
            if (bc != null)
            {
                changed |= !bc.isTrigger;
                bc.isTrigger = true;
            }

            if (tp.respawnMarker == null)
            {
                bool isFixed = false;
                foreach (Transform child in tp.gameObject.transform)
                {
                    var hrm = child.gameObject.GetComponent<HazardRespawnMarker>();
                    if (hrm != null)
                    {
                        tp.respawnMarker = hrm;
                        isFixed = true;
                        changed = true;
                        break;
                    }
                }

                if (!isFixed) Debug.LogError($"{tp.name} is missing its HazardRespawnMarker");
            }

            return ChangedResult(changed);
        }
    }
}