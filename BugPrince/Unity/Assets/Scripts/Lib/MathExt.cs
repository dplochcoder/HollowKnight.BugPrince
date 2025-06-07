using System;
using System.Collections.Generic;
using UnityEngine;

namespace BugPrince.Scripts.Lib
{
    public static class MathExt
    {
        public const float DEFAULT_GRAVITY = 36;

        public static float AdvanceFloat(this ref float self, float delta, float target)
        {
            float before = Mathf.Sign(target - self);
            float after = Mathf.Sign(target - self - delta);
            if (before != after)
            {
                var remaining = Mathf.Abs(target - self - delta);
                self = target;
                return remaining;
            }
            else
            {
                self += delta;
                return 0;
            }
        }

        public static bool UpdateFloat(this ref float self, float value)
        {
            if (Mathf.Abs(self - value) < 0.001f) return false;

            self = value;
            return true;
        }

        public static float Snap(float f, float epsilon) => epsilon * Mathf.Round(f / epsilon);

        public static bool NeedsSnap(float f, float epsilon) => Mathf.Abs(f - Snap(f, epsilon)) > 1e-6f;

        public static Vector3 Snap(Vector3 v, float epsilon) => new Vector3(Snap(v.x, epsilon), Snap(v.y, epsilon), Snap(v.z, epsilon));

        public static bool NeedsSnap(Vector3 v, float epsilon) => NeedsSnap(v.x, epsilon) || NeedsSnap(v.y, epsilon) || NeedsSnap(v.z, epsilon);

        public static (int, int) Order(int a, int b) => a < b ? (a, b) : (b, a);

        public static IEnumerable<int> Seq(int start, int limit, int step = 1)
        {
            (start, limit) = Order(start, limit);
            if (step > 0) for (int i = start; i < limit; i += step) yield return i;
            else
            {
                int max = start;
                while (max + start < limit) max -= step;
                for (int i = max; i >= start; i += step) yield return i;
            }
        }

        public static bool Snap(BoxCollider2D box, float epsilon)
        {
            // TODO: Always treat trigger as first.
            BoxCollider2D[] colliders = box.gameObject.GetComponents<BoxCollider2D>();
            bool first = box == colliders[0];

            var bounds = box.bounds;
            float x1 = Snap(bounds.min.x, epsilon);
            float y1 = Snap(bounds.min.y, epsilon);
            float x2 = Snap(bounds.max.x, epsilon);
            float y2 = Snap(bounds.max.y, epsilon);

            bool changed = false;
            Vector3 center = new Vector3((x1 + x2) / 2, (y1 + y2) / 2);
            if (first)
            {
                changed |= box.offset.sqrMagnitude >= 1e-6f;
                box.offset = Vector2.zero;
            }
            else
            {
                Vector2 target = center - box.gameObject.transform.position;
                changed |= (box.offset - target).sqrMagnitude >= 1e-6f;
                box.offset = target;
            }

            var targetSize = new Vector2(x2 - x1, y2 - y1);
            changed |= (box.size - targetSize).sqrMagnitude >= 1e-6f;
            box.size = targetSize;

            if (first)
            {
                changed |= (box.gameObject.transform.position - center).sqrMagnitude >= 1e-6f;
                box.gameObject.transform.position = center;
            }

            return changed;
        }

        public static bool Snap(PolygonCollider2D poly, float epsilon)
        {
            bool changed = false;

            var points = poly.points;
            List<Vector2> newPoints = new List<Vector2>();
            foreach (var point in points)
            {
                if (NeedsSnap(point, epsilon))
                {
                    changed = true;
                    newPoints.Add(Snap(point, epsilon));
                }
                else newPoints.Add(point);
            }

            poly.points = newPoints.ToArray();
            return changed;
        }

        public static void ResetZero(Transform transform)
        {
            var orig = transform.position;
            foreach (Transform child in transform)
            {
                var cPos = child.position;
                child.position = child.position + orig;
            }
            transform.position = Vector3.zero;
        }

        public static bool UpdatePosition(this Transform transform, Vector3 pos)
        {
            var diff = pos - transform.position;
            if (diff.sqrMagnitude < 1e-6f) return false;

            transform.position = pos;
            return true;
        }

        public static bool UpdateLocalScale(this Transform transform, Vector3 scale)
        {
            var diff = scale - transform.localScale;
            if (diff.sqrMagnitude < 1e-6f) return false;

            transform.localScale = scale;
            return true;
        }

        public static bool UpdateLocalRotation(this Transform transform, Quaternion rotation)
        {
            var dist = transform.localRotation * Quaternion.Inverse(rotation);
            var ea = dist.eulerAngles;
            if (Mathf.Abs(ea.x) + Mathf.Abs(ea.y) + Mathf.Abs(ea.z) < 1) return false;

            transform.localRotation = rotation;
            return true;
        }

        public static bool UpdateSize(this SpriteRenderer spriteRenderer, Vector2 size)
        {
            var old = spriteRenderer.size;

            bool changed = false;
            changed |= old.x.UpdateFloat(size.x);
            changed |= old.y.UpdateFloat(size.y);

            if (changed) spriteRenderer.size = size;
            return changed;
        }

        private const float CAMERA_DEPTH = 38.1f;
        public static float CameraScale(float z) => CAMERA_DEPTH / (z + CAMERA_DEPTH);

        public static void DepthAdjust(this Transform self)
        {
            self.localScale = self.localScale / CameraScale(self.position.z);
        }

        public static Vector2 To2d(this Vector3 v) => new Vector2(v.x, v.y);

        public static Vector3 To3d(this Vector2 v) => new Vector3(v.x, v.y, 0);

        public static Vector2Int To2d(this Vector3Int v) => new Vector2Int(v.x, v.y);

        public static Vector3Int To3d(this Vector2Int v) => new Vector3Int(v.x, v.y, 0);

        public static Vector2 Interpolate(this Vector2 a, float pct, Vector2 b) => a + (b - a) * pct;

        public static Vector3 Interpolate(this Vector3 a, float pct, Vector3 b) => a + (b - a) * pct;

        public static void UpdateAngle(this ref float self, float change) => self = (self + change) % 360;

        public static Quaternion RadialToQuat(float x, float y, float degOffset) => (VecToAngle(x, y) + degOffset).AsAngleToQuat();

        public static float ToAngle(this Quaternion q) => q.eulerAngles.z;

        public static float VecToAngle(float x, float y) => Mathf.Atan2(y, x) * Mathf.Rad2Deg;

        public static float VecToAngle(Vector2 vec) => VecToAngle(vec.x, vec.y);

        public static float ToAngle(this Vector2 vec) => VecToAngle(vec);

        public static float ZeroDevide(float num, float denom) => Mathf.Abs(denom) >= 0.000001f ? num / denom : Mathf.Infinity;

        public static Vector2 AsAngleToVec(this float angle) => new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        public static Quaternion AsAngleToQuat(this float angle) => Quaternion.AngleAxis(angle, Vector3.forward);

        public static Quaternion AsAngleToQuat(this float angle, float degOffset) => (angle + degOffset).AsAngleToQuat();

        public static Quaternion RadialToQuat(float x, float y) => RadialToQuat(x, y, 0);

        public static Quaternion RadialVecToQuat(Vector2 vec) => RadialToQuat(vec.x, vec.y);

        public static Vector2 RadialVec(float dist, float angle) => new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad) * dist, Mathf.Sin(angle * Mathf.Deg2Rad) * dist);

        public static Quaternion RadialVecToQuat(Vector2 vec, float degOffset) => RadialToQuat(vec.x, vec.y, degOffset);

        public static bool IsAngleBetween(float angle, float min, float max)
        {
            while (angle < min) angle += 360;
            while (angle > max) angle -= 360;
            return angle >= min && angle <= max;
        }

        // TODO: Remove
        public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

        public static float ClampAngle(float angle, float min, float max)
        {
            while (angle < min) angle += 360;
            while (angle > max) angle -= 360;
            if (angle < min - 180) return max;
            else if (angle < min || angle > max + 180) return min;
            else if (angle > max) return max;
            else return angle;
        }

        public static float ClampAngle(float angle) => ClampAngle(angle, 0, 360);

        public static float NormalizeAngle(float angle, float min, float max)
        {
            while (angle < min) angle += 360;
            while (angle > max) angle -= 360;
            return angle;
        }

        public static float NormalizeAngle(float angle) => NormalizeAngle(angle, 0, 360);

        public static Vector2 RandomInCircle(Vector2 center, float radius)
        {
            float rAngle = UnityEngine.Random.Range(0f, 360f);
            float rRadius = Mathf.Sqrt(UnityEngine.Random.Range(0f, 1f)) * radius;
            return center + rAngle.AsAngleToVec() * rRadius;
        }

        public static bool HasMinimumDistance(this RaycastHit2D hit, float distance) => hit.collider == null || hit.distance >= distance;

        public static bool CyclicFloatBetween(float test, float min, float max, float? cycle)
        {
            if (cycle == null || min <= max) return test >= min && test <= max;
            test = test % cycle.Value;
            return test >= min || test <= max;
        }

        public static (int, T) SelectMin<T>(Func<T, T, int> comparer, T item1, params T[] rest)
        {
            int index = 0;
            T min = item1;
            for (int i = 0; i < rest.Length; i++)
            {
                if (comparer(min, rest[i]) > 0)
                {
                    index = i + 1;
                    min = rest[i];
                }
            }

            return (index, min);
        }

        public static (int, T) SelectMin<T>(T item1, params T[] rest) => SelectMin(Comparer<T>.Default.Compare, item1, rest);

        // Returns all real 'x' for which ax^2 + bx + c = 0
        public static IEnumerable<float> SolveQuadratic(float a, float b, float c)
        {
            // x = (-b +- sqrt(b^2 - 4ac)) / 2a
            float det = b * b - 4 * a * c;
            if (Mathf.Abs(det) <= 1e-6f) yield return -b / (2 * a);
            else if (det < 0) yield break;
            else
            {
                float s = Mathf.Sqrt(det);
                yield return (s - b) / (2 * a);
                yield return (s + b) / (2 * a);
            }
        }

        // Compute initial angle for a parabolic arc from arc->dest with gravity.  Null if impossible.
        public static Vector2? SolveArc(Vector2 src, Vector2 dest, float velocity, float gravity = DEFAULT_GRAVITY)
        {
            var dx = dest.x - src.x;
            var dy = dest.y - src.y;
            var v = velocity;
            var v2 = velocity * velocity;
            var v4 = v2 * v2;
            var g = gravity;
            var g2 = gravity * gravity;

            float det1 = v4 - 2 * dy * g * v2 - dx * dx * g2;
            if (det1 < 0) return null;

            float p1 = v2 / g2 - dy / g;
            float p2 = Mathf.Sqrt(det1) / g2;
            float det2a = p1 - p2;
            float det2b = p1 + p2;
            if (det2a < 0 && det2b < 0) return null;

            float det = det2a < 0 ? det2b : det2a;
            float t = Mathf.Sqrt(det * 2);

            float vx = dx / t;
            if (vx > v) return null;

            float vy = Mathf.Sqrt(v2 - vx * vx);
            return new Vector2(vx, vy);
        }

        public static void UpdateBuzzVelocity(this Rigidbody2D self, Vector2 target, float accel, float speedLimit)
        {
            var delta = target.To3d() - self.gameObject.transform.position;
            var dist = delta.magnitude;

            // Slam brakes
            // d = vt/2; t = v/a; d = v^2/2a
            // v^2 = 2ad; v = sqrt(2ad)
            var maxSpeed = Mathf.Sqrt(2 * accel * dist);
            var speed = Mathf.Min(maxSpeed, speedLimit);
            var targetVelocity = (delta.normalized * speed).To2d();

            // Accel towards target.
            var velocity = self.velocity;

            var accelDelta = accel * Time.deltaTime;
            if ((targetVelocity - velocity).magnitude <= accelDelta) self.velocity = targetVelocity;
            else self.velocity = velocity + (targetVelocity - velocity).normalized * accelDelta;
        }

        public static float DotProduct(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;

        // Point on line segment [a,b] closest to t, and the square of the distance.
        public static (Vector2, float) ClosestToLineSegment(Vector2 a, Vector2 b, Vector2 t)
        {
            var at = t - a;
            float dist1 = at.sqrMagnitude;
            var bt = t - b;
            float dist2 = bt.sqrMagnitude;
            var ab = b - a;
            var segSq = ab.sqrMagnitude;
            if (segSq < 1e-6f) return dist1 < dist2 ? (a, dist1) : (b, dist2);

            var unit = ab.normalized;
            var ap = DotProduct(at, unit) * unit;
            var dot = DotProduct(ap, ab);
            if (dot > 0 && dot < segSq)
            {
                var i = a + ap;
                return (i, (t - i).sqrMagnitude);
            }

            return dist1 < dist2 ? (a, dist1) : (b, dist2);
        }

        public static IEnumerable<float> EqualAngles(float start, int num)
        {
            for (int i = 0; i < num; i++) yield return start + i * 360f / num;
        }

        public static IEnumerable<float> EqualAngles(int num) => EqualAngles(0, num);

        public static (int, int, int) SplitGeo(int geo)
        {
            int small = geo % 5;
            if (small <= 1 && geo >= 5 + small) small += 5;
            geo -= small;

            int medium = (geo % 25) / 5;
            if (medium <= 1 && geo >= 25 + 5 * medium) medium += 5;
            geo -= medium * 5;

            int large = geo / 25;

            return (small, medium, large);
        }
    }
}