using UnityEngine;

namespace Game.Runtime
{
    /// <summary>
    /// 让物体跨场景存活（DontDestroyOnLoad）。挂在需要跨场景的单例根物体上（如 LoadingScreen）。
    /// </summary>
    public class PersistentRoot : MonoBehaviour
    {
        protected void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
