using ItemChanger;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Tags;

internal class DeployerTag : Tag
{
    public Deployer? Deployer;

    public override void Load(object parent)
    {
        base.Load(parent);
        if (Deployer != null) Events.AddSceneChangeEdit(Deployer.SceneName, Deploy);
    }

    public override void Unload(object parent)
    {
        base.Unload(parent);
        if (Deployer != null) Events.RemoveSceneChangeEdit(Deployer.SceneName, Deploy);
    }

    private void Deploy(Scene scene) => Deployer?.Deploy();
}
