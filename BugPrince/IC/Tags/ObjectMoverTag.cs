using ItemChanger;
using Modding.Converters;
using Newtonsoft.Json;
using SFCore.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Tags;

internal class ObjectMoverTag : Tag
{
    public string SceneName = "";
    public string ObjectPath = "";
    [JsonConverter(typeof(Vector3Converter))] public Vector3 Move;

    public override void Load(object parent)
    {
        base.Load(parent);
        Events.AddSceneChangeEdit(SceneName, MoveObject);
    }

    public override void Unload(object parent)
    {
        base.Unload(parent);
        Events.RemoveSceneChangeEdit(SceneName, MoveObject);
    }

    private void MoveObject(Scene scene) => scene.Find(ObjectPath).transform.Translate(Move);
}
