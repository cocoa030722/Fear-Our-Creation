using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    /// <summary>
    /// 입력 액션 모음(WASD / 마우스 / Space / R). 리바인딩을 염두에 두고 InputAction으로 정의한다.
    /// </summary>
    public class GameInput
    {
        static GameInput _instance;
        public static GameInput Instance => _instance ??= new GameInput();

        public readonly InputAction Move;
        public readonly InputAction Point;
        public readonly InputAction Attack;
        public readonly InputAction Restart;
        public readonly InputAction Interact;
        public readonly InputAction DebugKill;

        GameInput()
        {
            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            Point = new InputAction("Point", InputActionType.Value, "<Mouse>/position");
            Attack = new InputAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
            Restart = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");
            Interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/space");
            DebugKill = new InputAction("DebugKill", InputActionType.Button, "<Keyboard>/k");

            Move.Enable();
            Point.Enable();
            Attack.Enable();
            Restart.Enable();
            Interact.Enable();
            DebugKill.Enable();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _instance = null;
    }
}
