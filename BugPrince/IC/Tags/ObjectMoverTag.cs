using ItemChanger.Extensions;
using Modding.Converters;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Tags;

internal class ObjectMoverTag : SceneModifierTag
{
    public string SceneName = "";
    public string ObjectPath = "";
    [JsonConverter(typeof(Vector3Converter))] public Vector3 Move;

    protected override string GetSceneName() => SceneName;

    protected override void ModifyScene(Scene scene) => scene.FindGameObject(ObjectPath)?.transform.Translate(Move);
}
