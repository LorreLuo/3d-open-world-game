using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Character
{
    /// <summary>
    /// 对角色模型的轻量外观定制：给指定部件换色、按名称缝合护甲（骨骼重绑定，同 Demo 的 SkinnedMeshStitcher 逻辑）。
    /// 创建场景预览与游戏内玩家共用。
    /// </summary>
    public class CharacterCustomization
    {
        protected readonly Transform m_Root;
        protected readonly Dictionary<string, SkinnedMeshRenderer> m_Renderers = new Dictionary<string, SkinnedMeshRenderer>();
        protected readonly Dictionary<string, Material> m_Materials = new Dictionary<string, Material>();
        protected readonly List<GameObject> m_Stitched = new List<GameObject>();

        protected SkinnedMeshRenderer m_Body;

        public CharacterCustomization(Transform root)
        {
            m_Root = root;
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var r in renderers) {
                if (m_Renderers.ContainsKey(r.name) == false) { m_Renderers.Add(r.name, r); }
            }
            if (m_Renderers.TryGetValue("Body", out var body)) { m_Body = body; }
        }

        public void ApplyColor(string partName, Color color)
        {
            if (m_Renderers.TryGetValue(partName, out var renderer) == false || renderer == null) {
                Debug.LogWarning($"[Game.Character] 未找到部件渲染器: {partName}");
                return;
            }
            // 每个部件只实例化一次材质，避免污染共享材质
            if (m_Materials.TryGetValue(partName, out var mat) == false) {
                mat = renderer.material;
                m_Materials.Add(partName, mat);
            }
            // Demo 的 Toon 着色器颜色属性是 _BaseColorRGBOutlineWidthA（非标准 _Color），需按属性名写入
            if (mat.HasProperty("_BaseColorRGBOutlineWidthA")) {
                var v = mat.GetVector("_BaseColorRGBOutlineWidthA");
                mat.SetVector("_BaseColorRGBOutlineWidthA", new Vector4(color.r, color.g, color.b, v.w));
            } else {
                mat.color = color;
            }
        }

        public void ApplyOutfit(string outfitId)
        {
            // 清掉已缝合的护甲
            foreach (var go in m_Stitched) {
                if (go != null) { Object.Destroy(go); }
            }
            m_Stitched.Clear();

            if (string.IsNullOrEmpty(outfitId) || outfitId == "None") { return; }

            var prefab = Resources.Load<GameObject>("Outfits/" + outfitId + "Armor");
            if (prefab == null) {
                Debug.LogWarning($"[Game.Character] 未找到护甲预制体: Outfits/{outfitId}Armor");
                return;
            }
            if (m_Body == null) {
                Debug.LogWarning("[Game.Character] 未找到 Body 渲染器，无法缝合护甲。");
                return;
            }

            var instance = Object.Instantiate(prefab, m_Root);
            var smrs = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var smr in smrs) {
                smr.rootBone = m_Body.rootBone;
                smr.bones = m_Body.bones;
            }
            m_Stitched.Add(instance);
        }
    }
}
