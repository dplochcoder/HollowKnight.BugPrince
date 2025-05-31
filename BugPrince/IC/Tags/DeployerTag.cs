using ItemChanger;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Tags;

internal class DeployerTag : SceneModifierTag
{
    public Deployer? Deployer;

    protected override string GetSceneName() => Deployer?.SceneName ?? "";

    protected override void ModifyScene(Scene scene) => Deployer?.Deploy();
}
