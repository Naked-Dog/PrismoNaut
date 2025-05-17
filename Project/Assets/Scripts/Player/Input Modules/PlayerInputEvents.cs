using UnityEngine.InputSystem;

namespace PlayerSystem
{
    public struct OnHorizontalInput
    {
        public float amount;
        public OnHorizontalInput(float amount) { this.amount = amount; }
    }
    public struct OnVerticalInput
    {
        public float amount;
        public OnVerticalInput(float amount) { this.amount = amount; }
    }
    public struct OnJumpInput
    {
        public InputAction jumpInputAction;
        public OnJumpInput(InputAction jumpInputAction) { this.jumpInputAction = jumpInputAction; }
    }
    public struct OnLookDownInput
    {
        public bool toggle;
        public OnLookDownInput(bool toggle) { this.toggle = toggle; }
    }
    public struct OnSquarePowerInput { }
    public struct OnTrianglePowerInput { }
    public struct OnCirclePowerInput { }
    public struct OnPauseInput { }
    public struct OnInteractionInput { }
    public struct RequestPlayerInputs { }
    public struct RequestStopPlayerInputs { }
    public struct RequestEnableDialogueInputs { }
    public struct RequestDisableDialogueInputs { }
}

