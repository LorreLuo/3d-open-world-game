using UnityEngine;

/// <summary>
/// 角色创建的自定义数据（换色/换装），经 Spark 存档插件在场景间传递并随存档槽持久化。
/// 注意：必须放在全局命名空间——Spark 存档按 Type.Name（简单名）序列化/反序列化，
/// 带命名空间的类型无法被 GetTypeByName 解析。
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
