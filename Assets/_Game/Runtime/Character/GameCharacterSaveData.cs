using UnityEngine;

namespace Game.Runtime.Character
{
    /// <summary>
    /// 角色创建的自定义数据（换色/换装），经 Spark 存档插件在场景间传递；SP2 起随存档位持久化。
    /// </summary>
    [System.Serializable]
    public class GameCharacterSaveData : SaveDataEntry
    {
        public Color hairColor = new Color(0.4f, 0.3f, 0.25f);
        public Color shirtColor = new Color(0.9f, 0.9f, 0.9f);
        public Color bootsColor = new Color(0.35f, 0.2f, 0.15f);
        public string outfitId = "None"; // None | Leather | Knight

        public override void InitializeDefaults()
        {
            hairColor = new Color(0.4f, 0.3f, 0.25f);
            shirtColor = new Color(0.9f, 0.9f, 0.9f);
            bootsColor = new Color(0.35f, 0.2f, 0.15f);
            outfitId = "None";
        }
    }

    /// <summary>
    /// 注册角色自定义存档类型（进程启动后执行一次）。
    /// </summary>
    public static class GameCharacterSaveDataRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Register()
        {
            var save = Spark.GetPlugin<ISaveDataPlugin>();
            if (save != null) { save.RegisterSaveDataType<GameCharacterSaveData>(); }
        }
    }
}
