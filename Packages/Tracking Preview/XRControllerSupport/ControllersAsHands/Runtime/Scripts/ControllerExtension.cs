#if ENABLE_INPUT_SYSTEM

using UnityEngine.InputSystem;
namespace Leap.Unity.Controllers
{
    public static class ControllerExtension
    {
        /// <summary>
        /// Returns whether a controller is active based on the whether the controller is null,
        /// if the controller has been added and if the controller is tracked
        /// </summary>
        public static bool IsControllerActive(this TrackedDevice controller)
        {
            return controller != null && controller.added && controller.isTracked.ReadValue() == 1;
        }
    }
}
#endif