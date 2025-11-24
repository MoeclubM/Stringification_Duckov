using UnityEngine;
using Duckov.Modding;

namespace Stringification.Components
{
    public class InputManager
    {
        public KeyCode ToggleKey { get; set; } = KeyCode.LeftControl;
        public KeyCode JumpKey { get; set; } = KeyCode.X;
        public float FlightActivationSpeed { get; set; } = 0.5f;

        private bool hasDoubleJumped = false;

        public void Reset()
        {
            hasDoubleJumped = false;
        }

        public void ResetDoubleJump()
        {
            hasDoubleJumped = false;
        }

        public bool CheckToggleInput()
        {
            if (Cursor.visible) return false;
            return Input.GetKeyDown(ToggleKey);
        }

        public bool CheckJumpInput()
        {
            if (Cursor.visible) return false;
            return Input.GetKeyDown(JumpKey);
        }

        public bool CheckFireInput()
        {
            if (Cursor.visible) return false;
            return Input.GetMouseButtonDown(0);
        }

        public float GetHorizontalInput()
        {
            if (Cursor.visible) return 0f;
            return Input.GetAxis("Horizontal");
        }

        public bool CanDoubleJump(bool isGrounded)
        {
            if (!isGrounded && !hasDoubleJumped)
            {
                hasDoubleJumped = true;
                return true;
            }
            return false;
        }

        public bool ShouldActivateFlight(Rigidbody rb)
        {
            if (rb == null) return false;
            
            Vector3 horizVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            if (horizVel.magnitude > FlightActivationSpeed)
            {
                return true;
            }

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (new Vector2(h, v).magnitude > 0.1f) return true;

            return false;
        }
    }
}
