using UnityEngine;

namespace Game.Runtime.Character
{
    /// <summary>
    /// 玩家出生时读取角色创建的自定义数据并应用外观（挂在玩家预制体根上）。
    /// </summary>
    public class PlayerCustomizationApplier : MonoBehaviour
    {
        protected virtual void Start()
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
    }
}
