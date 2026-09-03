using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Runtime.Player
{
    /// <summary>
    /// 直接订阅输入动作驱动 SparkThirdPersonController。
    /// 不依赖 PlayerInput.SendMessages，避免与 Opsive 的输入组件冲突或消息时序问题，
    /// 保证键盘/手柄输入确定性送达控制器。
    /// </summary>
    [RequireComponent(typeof(SparkThirdPersonController))]
    public class SparkInputFeeder : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";

        private SparkThirdPersonController _controller;
        private InputAction _move;
        private InputAction _look;
        private InputAction _jump;
        private InputAction _sprint;
        private InputAction _walk;

        /// <summary>是否已成功绑定 Move 动作（供测试/诊断用）。</summary>
        public bool IsWired => _move != null;

        protected virtual void Awake()
        {
            _controller = GetComponent<SparkThirdPersonController>();

            if (inputActions == null) {
                Debug.LogWarning($"[SparkInputFeeder] {gameObject.name} 缺少 inputActions。", this);
                return;
            }

            var map = inputActions.FindActionMap(actionMapName, throwIfNotFound: false);
            if (map == null) {
                Debug.LogWarning($"[SparkInputFeeder] {gameObject.name} 找不到动作映射 '{actionMapName}'。", this);
                return;
            }

            _move = map.FindAction("Move", throwIfNotFound: false);
            _look = map.FindAction("Look", throwIfNotFound: false);
            _jump = map.FindAction("Jump", throwIfNotFound: false);
            _sprint = map.FindAction("Sprint", throwIfNotFound: false);
            _walk = map.FindAction("Walk", throwIfNotFound: false);
        }

        protected virtual void OnEnable()
        {
            Hook(_move, OnMovePerformed, OnMoveCanceled);
            Hook(_look, OnLookPerformed, OnLookCanceled);
            Hook(_jump, OnJumpPerformed, OnJumpCanceled);
            Hook(_sprint, OnSprintPerformed, OnSprintCanceled);
            Hook(_walk, OnWalkPerformed, null);
        }

        protected virtual void Start()
        {
            EnsureDevicesBound();
        }

        /// <summary>
        /// 兜底：若 PlayerInput 未绑定任何设备（devices 为空会导致动作 controls 为空、键盘输入完全不生效），
        /// 显式切换到 KeyboardMouse 方案并绑定当前键盘/鼠标。
        /// </summary>
        private void EnsureDevicesBound()
        {
            var pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (pi == null) return;
            if (pi.devices.Count > 0) return;

            var kb = Keyboard.current;
            var ms = Mouse.current;
            if (kb == null && ms == null) return;

            try {
                if (kb != null && ms != null) {
                    pi.SwitchCurrentControlScheme("KeyboardMouse", kb, ms);
                } else if (kb != null) {
                    pi.SwitchCurrentControlScheme("KeyboardMouse", kb);
                }
            } catch (System.Exception e) {
                Debug.LogWarning($"[SparkInputFeeder] 绑定键盘/鼠标设备失败: {e.Message}", this);
            }
        }

        protected virtual void OnDisable()
        {
            Unhook(_move, OnMovePerformed, OnMoveCanceled);
            Unhook(_look, OnLookPerformed, OnLookCanceled);
            Unhook(_jump, OnJumpPerformed, OnJumpCanceled);
            Unhook(_sprint, OnSprintPerformed, OnSprintCanceled);
            Unhook(_walk, OnWalkPerformed, null);
        }

        static void Hook(InputAction action, System.Action<InputAction.CallbackContext> performed, System.Action<InputAction.CallbackContext> canceled)
        {
            if (action == null) return;
            if (performed != null) action.performed += performed;
            if (canceled != null) action.canceled += canceled;
            action.Enable();
        }

        static void Unhook(InputAction action, System.Action<InputAction.CallbackContext> performed, System.Action<InputAction.CallbackContext> canceled)
        {
            if (action == null) return;
            if (performed != null) action.performed -= performed;
            if (canceled != null) action.canceled -= canceled;
            action.Disable();
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            if (_controller != null) _controller.SetMovementInput(ctx.ReadValue<Vector2>());
        }

        private void OnMoveCanceled(InputAction.CallbackContext ctx)
        {
            if (_controller != null) _controller.SetMovementInput(Vector2.zero);
        }

        private void OnLookPerformed(InputAction.CallbackContext ctx)
        {
            if (_controller != null) _controller.SetCameraInput(ctx.ReadValue<Vector2>());
        }

        private void OnLookCanceled(InputAction.CallbackContext ctx)
        {
            if (_controller != null) _controller.SetCameraInput(Vector2.zero);
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            if (_controller != null) _controller.SetLeapInput(true);
        }

        private void OnJumpCanceled(InputAction.CallbackContext ctx)
        {
            if (_controller != null) _controller.SetLeapInput(false);
        }

        private void OnSprintPerformed(InputAction.CallbackContext ctx)
        {
            if (_controller != null) _controller.SetSprintInput(true);
        }

        private void OnSprintCanceled(InputAction.CallbackContext ctx)
        {
            if (_controller != null) _controller.SetSprintInput(false);
        }

        private void OnWalkPerformed(InputAction.CallbackContext ctx)
        {
            if (_controller != null && ctx.ReadValueAsButton()) _controller.SetWalkInput(true);
        }
    }
}
