using UnityEngine;

/// <summary>
/// 世界进度存档数据：场景 + 玩家位置。sceneName 为空表示尚未写入过进度（新游戏）。
/// 注意：必须放在全局命名空间——Spark 存档按 Type.Name（简单名）序列化/反序列化。
/// </summary>
[System.Serializable]
public class GameProgressSaveData : SaveDataEntry
{
    public string sceneName = "";
    public Vector3 playerPosition;

    public override void InitializeDefaults()
    {
        sceneName = "";
        playerPosition = Vector3.zero;
    }

    public override bool ValidateData()
    {
        return sceneName != null;
    }
}

/// <summary>
/// 注册进度存档类型（进程启动后执行一次）。
/// </summary>
public static class GameProgressSaveDataRegistration
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void Register()
    {
        var save = Spark.GetPlugin<ISaveDataPlugin>();
        if (save != null) { save.RegisterSaveDataType<GameProgressSaveData>(); }
    }
}
