#if ENABLE_INPUT_SYSTEM

using UnityEngine.InputSystem;
namespace Leap.Unity.Controllers
{
    public static class ControllerExtension
    {
        public static bool IsControllerActive(this TrackedDevice controller)
        {
            return controller != null && controller.added && controller.isTracked.ReadValue() == 1;
        }
    }
}
#endif