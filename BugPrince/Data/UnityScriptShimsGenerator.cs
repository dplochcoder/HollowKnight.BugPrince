using BugPrince.Scripts.InternalLib;
using BugPrince.Util;
using PurenailCore.SystemUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BugPrince.Data;

internal static class UnityScriptShimsGenerator
{
    internal static void GenerateUnityShims(string root) => GenerateDirectory($"{root}/UnityScriptShims/Scripts/Generated", GenerateUnityShimsImpl);

    private static void GenerateDirectory(string dir, Action<string> generator)
    {
        string gen = dir;
        string gen2 = $"{dir}.tmp";
        if (Directory.Exists(gen2)) Directory.Delete(gen2, true);
        Directory.CreateDirectory(gen2);

        generator(gen2);

        // On success, swap the dirs.
        if (Directory.Exists(gen)) Directory.Delete(gen, true);
        Directory.Move(gen2, gen);
    }

    private static void GenerateUnityShimsImpl(string root)
    {
        typeof(DataUpdater).Assembly.GetTypes().Where(t => t.IsDefined(typeof(Shim), false)).ForEach(type =>
        {
            try { GenerateShimFile(type, root); }
            catch (Exception ex) { throw new Exception($"Failed to generate {type.Name}", ex); }
        });
    }

    private static HashSet<Type> validTypes = [];

    private static void ValidateType(Type type)
    {
        if (validTypes.Contains(type)) return;

        if (type.Assembly.GetName().Name == "Assembly-CSharp") throw new ArgumentException($"Cannot reference Assembly-CSharp type {type.Name} directly");
        type.GenericTypeArguments.ForEach(ValidateType);
        validTypes.Add(type);
    }

    private static void GenerateShimFile(Type type, string dir)
    {
        string ns = type.Namespace;
        string origNs = ns;
        if (ns == "BugPrince.Scripts") ns = "";
        else if (ns.ConsumePrefix("BugPrince.Scripts.", out var trimmed)) ns = trimmed;

        string pathDir = ns.Length == 0 ? $"{dir}" : $"{dir}/{ns.Replace('.', '/')}";
        string path = $"{pathDir}/{type.Name}.cs";

        var baseType = type.GetCustomAttribute<Shim>()?.baseType ?? typeof(MonoBehaviour);
        ValidateType(baseType);

        string header;
        List<string> fieldStrs = [];
        List<string> attrStrs = [];
        if (type.IsEnum)
        {
            header = $"enum {type.Name}";
            foreach (var v in type.GetEnumValues()) fieldStrs.Add($"{v},");
        }
        else
        {
            foreach (var rc in type.GetCustomAttributes<RequireComponent>())
            {
                if (rc.m_Type0 != null) attrStrs.Add(RequireComponentStr(origNs, rc.m_Type0));
                if (rc.m_Type1 != null) attrStrs.Add(RequireComponentStr(origNs, rc.m_Type1));
                if (rc.m_Type2 != null) attrStrs.Add(RequireComponentStr(origNs, rc.m_Type2));
            }

            header = $"class {type.Name} : {PrintType(origNs, baseType)}";
            foreach (var f in type.GetFields().Where(f => f.IsDefined(typeof(ShimField), true)))
            {
                List<string> fattrStrs = [];
                foreach (var h in f.GetCustomAttributes<HeaderAttribute>()) fattrStrs.Add($"[UnityEngine.Header(\"{h.header}\")]");

                var fAttr = f.GetCustomAttribute<ShimField>();
                var defaultValue = fAttr.DefaultValue;
                string dv = defaultValue != null ? $" = {defaultValue}" : "";
                fieldStrs.Add($"{JoinIndented(fattrStrs, 8)}public {PrintType(origNs, f.FieldType)} {f.Name}{dv};");
            }
        }

        var content = $@"namespace {origNs}
{{
    {JoinIndented(attrStrs, 4)}public {header}
    {{
        {JoinIndented(fieldStrs, 8)}
    }}
}}";

        WriteSourceCode(path, content);
    }


    private static string RequireComponentStr(string ns, Type type)
    {
        ValidateType(type);
        return $"[UnityEngine.RequireComponent(typeof({PrintType(ns, type)}))]";
    }

    private static void WriteSourceCode(string path, string content)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        File.WriteAllText(path, content.Replace("\r\n", "\n").Replace("\n", "\r\n"));
    }

    private static string Pad(string src, int indent)
    {
        var splits = src.Split('\n');
        for (int i = 1; i < splits.Length; i++) splits[i] = $"{new string(' ', indent)}{splits[i]}";
        return string.Join("", splits);
    }

    private static string JoinIndented(List<string> list, int indent) => string.Join("", list.Select(s => $"{Pad(s, indent)}\n{new string(' ', indent)}"));

    private static string PrintType(string ns, Type t)
    {
        ValidateType(t);
        string s = PrintTypeImpl(ns, t);

        if (s.ConsumePrefix($"{ns}.", out string trimmed)) return trimmed;
        else return s;
    }

    private static string PrintTypeImpl(string ns, Type t)
    {
        ValidateType(t);
        if (!t.IsGenericType)
        {
            if (t == typeof(bool)) return "bool";
            if (t == typeof(int)) return "int";
            if (t == typeof(float)) return "float";
            if (t == typeof(string)) return "string";

            return t.FullName;
        }

        string baseName = t.FullName;
        baseName = baseName.Substring(0, baseName.IndexOf('`'));
        List<string> types = t.GenericTypeArguments.Select(t => PrintType(ns, t)).ToList();
        return $"{baseName}<{string.Join(", ", types)}>";
    }
}
