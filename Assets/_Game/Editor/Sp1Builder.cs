using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// SP1 一键构建：数据库条目 → 玩家 prefab（含外观应用器）→ 创建场景 → 主菜单 → 构建设置。
    /// </summary>
    public static class Sp1Builder
    {
        [MenuItem("Game/Build/SP1 All")]
        public static void BuildAll()
        {
            UiPrefabBuilder.BuildAll();
            DatabaseGenerator.Generate();
            PlayerPrefabBuilder.Build();
            CharacterCreationSceneBuilder.Build();
            MainMenuSceneBuilder.Build();
            GameWorldMigrator.AddGameWorldBootstrap();
            ProjectVerifier.ConfigureBuildSettings();
            Debug.Log("SP1_BUILD_ALL_DONE");
        }
    }
}
