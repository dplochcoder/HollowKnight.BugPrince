using BugPrince.Data;
using ItemChanger;
using ItemChanger.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Tags;

internal abstract class SpriteCopiesTag : SceneModifierTag
{
    protected abstract IEnumerable<SpriteCopy> SpriteCopies();

    protected override void ModifyScene(Scene scene)
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
    protected override string GetSceneName() => SceneNames.Ruins2_Watcher_Room;

    protected override IEnumerable<SpriteCopy> SpriteCopies() => Data.SpriteCopies.GetLurienSecretSpriteCopies();
}
