using ItemChanger;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Tags;

internal abstract class SceneModifierTag : Tag
{
    public override void Load(object parent)
    {
        base.Load(parent);
        Events.AddSceneChangeEdit(GetSceneName(), ModifyScene);
    }

    public override void Unload(object parent)
    {
        base.Unload(parent);
        Events.RemoveSceneChangeEdit(GetSceneName(), ModifyScene);
    }

    protected abstract string GetSceneName();

    protected abstract void ModifyScene(Scene scene);
}
