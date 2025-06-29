using UnityEngine;

namespace BugPrince.Util;

internal class Mover : MonoBehaviour
{
    private Vector3 velocity;

    internal void SetVelocity(Vector3 velocity) => this.velocity = velocity;

    private void Update() => transform.position += velocity * Time.deltaTime;
}
