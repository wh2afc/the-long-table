using UnityEngine;
using UnityEngine.InputSystem;

namespace LongTable.Game
{
    /// <summary>Left and right along the green. A/D, arrow keys, or a gamepad stick.</summary>
    public sealed class PlayerController : MonoBehaviour
    {
        public float speed = 4.2f;
        public float minX = -15f;
        public float maxX = 15f;

        private CharacterView view;

        private void Awake() => view = GetComponent<CharacterView>();

        private void Update()
        {
            float input = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input += 1f;
            }
            var pad = Gamepad.current;
            if (pad != null) input += pad.leftStick.ReadValue().x;
            input = Mathf.Clamp(input, -1f, 1f);
            if (Mathf.Abs(input) < 0.15f) return;

            float x = Mathf.Clamp(view.X + input * speed * Time.deltaTime, minX, maxX);
            view.Place(x);
            view.Face(input);
        }
    }
}
