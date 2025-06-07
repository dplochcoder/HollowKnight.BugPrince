using System;
using UnityEngine;

namespace BugPrince.Scripts.InternalLib;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
internal class Shim : Attribute
{
    public readonly Type baseType;

    public Shim(Type? baseType = null)
    {
        this.baseType = baseType ?? typeof(MonoBehaviour);
    }
}

[AttributeUsage(AttributeTargets.Field)]
internal class ShimField : Attribute
{
    public readonly string? DefaultValue;

    public ShimField(string? defaultValue = null) => DefaultValue = defaultValue;
}
