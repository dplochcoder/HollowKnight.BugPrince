using BugPrince.Data;
using ItemChanger;
using ItemChanger.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Tags;

internal abstract class SpriteCopiesTag : Tag
{
    protected abstract string SceneName();

    protected abstract IEnumerable<SpriteCopy> SpriteCopies();

    public override void Load(object parent)
    {
        base.Load(parent);
        Events.AddSceneChangeEdit(SceneName(), ModifyScene);
    }

    public override void Unload(object parent)
    {
        base.Unload(parent);
        Events.RemoveSceneChangeEdit(SceneName(), ModifyScene);
    }

    private void ModifyScene(Scene scene)
    {
        Dictionary<string, GameObject?> templates = [];
        foreach (var spriteCopy in SpriteCopies())
        {
            if (!templates.TryGetValue(spriteCopy.OrigPath, out var template) || template == null)
            {
                template = scene.FindGameObject(spriteCopy.OrigPath);
                templates[spriteCopy.OrigPath] = template;
                if (template == null) continue;
            }

            var copy = Object.Instantiate(template, spriteCopy.Position, Quaternion.Euler(0, 0, spriteCopy.Rotation));
            copy.transform.localScale = spriteCopy.Scale;
        }
    }
}

internal class LurienSecretSpriteCopiesTag : SpriteCopiesTag
{
    protected override string SceneName() => SceneNames.Ruins2_Watcher_Room;

    protected override IEnumerable<SpriteCopy> SpriteCopies() => Data.SpriteCopies.GetLurienSecretSpriteCopies();
}
