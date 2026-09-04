using UnityEngine;

namespace Game.Runtime.Character
{
    /// <summary>
    /// 玩家出生时读取存档数据并应用：外观（换色/换装）+ 世界进度（位置）。
    /// </summary>
    public class PlayerCustomizationApplier : MonoBehaviour
    {
        protected virtual void Start()
        {
            ApplyCustomization();
            ApplyProgress();
        }

        protected virtual void ApplyCustomization()
        {
            var save = Spark.GetPlugin<ISaveDataPlugin>();
            var data = save != null ? save.GetSaveData<GameCharacterSaveData>() : null;
            if (data == null) { return; }

            var customization = new CharacterCustomization(transform);
            customization.ApplyColor("Hair", data.hairColor);
            customization.ApplyColor("Shirt", data.shirtColor);
            customization.ApplyColor("Boots", data.bootsColor);
            customization.ApplyOutfit(data.outfitId);
        }

        protected virtual void ApplyProgress()
        {
            var save = Spark.GetPlugin<ISaveDataPlugin>();
            var progress = save != null ? save.GetSaveData<GameProgressSaveData>() : null;
            if (progress == null || string.IsNullOrEmpty(progress.sceneName)) { return; }

            // CharacterController 直接设 transform.position 会被下一次 Move 覆盖回旧位置，
            // 需先禁用再启用（标准传送写法）。
            var cc = GetComponent<CharacterController>();
            if (cc != null) { cc.enabled = false; }
            transform.position = progress.playerPosition;
            if (cc != null) { cc.enabled = true; }
        }
    }
}
