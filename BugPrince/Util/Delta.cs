using System;
using System.Collections.Generic;

namespace BugPrince.Util;

// Usage:
//   T before, after;
//   IDelta<T> delta;
//   delta.Calculate(before, after);
//   delta.Apply(before);  // before is now equal to after
public interface IDelta<T>
{
    // Set this object's state to the diff from before->after.
    void Calculate(T after, T before);

    // Apply this object's state to src.
    void Apply(T src);
}

public class ListDelta<T> : IDelta<List<T>>
{
    public List<(int, T)> Changes = [];
    public int NewSize;

    public void Calculate(List<T> after, List<T> before)
    {
        Changes.Clear();

        NewSize = after.Count;
        int min = Math.Min(after.Count, before.Count);
        for (int i = 0; i < min; i++) if (!EqualityComparer<T>.Default.Equals(after[i], before[i])) Changes.Add((i, after[i]));
        for (int i = min; i < after.Count; i++) Changes.Add((i, after[i]));
    }

    public void Apply(List<T> src)
    {
        if (src.Count > NewSize) src.RemoveRange(NewSize, src.Count - NewSize);
#pragma warning disable CS8604 // Possible null reference argument.
        while (src.Count < NewSize) src.Add(default);
#pragma warning restore CS8604 // Possible null reference argument.

        foreach (var (idx, value) in Changes) src[idx] = value;
    }
}

public class AppendOnlyListDelta<T> : IDelta<List<T>>
{
    public List<T> Changes = [];

    public void Calculate(List<T> after, List<T> before)
    {
        if (after.Count < before.Count) throw new ArgumentException($"AppendOnlyListDelta: {after.Count} < {before.Count}");

        Changes.Clear();
        for (int i = before.Count; i < after.Count; i++) Changes.Add(after[i]);
    }

    public void Apply(List<T> src) => src.AddRange(Changes);
}

public class HashSetDelta<T> : IDelta<HashSet<T>>
{
    public List<T> Add = [];
    public readonly List<T> Remove = [];

    public void Calculate(HashSet<T> after, HashSet<T> before)
    {
        Add.Clear();
        Remove.Clear();

        foreach (var item in after) if (!before.Contains(item)) Add.Add(item);
        foreach (var item in before) if (!after.Contains(item)) Remove.Add(item);
    }

    public void Apply(HashSet<T> src)
    {
        Remove.ForEach(t => src.Remove(t));
        Add.ForEach(t => src.Add(t));
    }
}

public class DictionaryDelta<K, V> : IDelta<Dictionary<K, V>>
{
    public List<(K, V)> Changes = [];
    public List<K> Removes = [];

    public void Calculate(Dictionary<K, V> after, Dictionary<K, V> before)
    {
        Changes.Clear();
        Removes.Clear();

        foreach (var e in after) if (!before.TryGetValue(e.Key, out var beforeValue) || !EqualityComparer<V>.Default.Equals(beforeValue, e.Value)) Changes.Add((e.Key, e.Value));
        foreach (var k in before.Keys) if (!after.ContainsKey(k)) Removes.Add(k);
    }

    public void Apply(Dictionary<K, V> src)
    {
        Removes.ForEach(k => src.Remove(k));
        foreach (var (k, v) in Changes) src[k] = v;
    }
}