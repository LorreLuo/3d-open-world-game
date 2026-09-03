using UnityEngine;

namespace Game.Runtime
{
    /// <summary>
    /// 让 LoadingScreen 跨场景存活，且去重：若已存在单例实例则销毁本实例（避免每个场景的实例都被 DDOL 导致残留可见加载界面）。
    /// 与 LoadingScreenManager 的单例逻辑配合，只有第一个实例 DDOL 并成为 Instance。
    /// </summary>
    public class LoadingScreenPersistent : MonoBehaviour
    {
        protected void Awake()
        {
            var instance = LoadingScreenManager.Instance;
            if (instance != null && instance.gameObject != gameObject) {
                // 已存在跨场景实例，本实例是重复项，直接销毁（不进 DDOL）
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
        }
    }
}
