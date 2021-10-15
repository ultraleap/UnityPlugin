using UnityEngine;

namespace Leap.Unity.InputModule
{
    /// <summary>
    /// Stores the settings for the input module
    /// </summary>
    public interface IInputModuleSettings
    {
        //The distance from the base of a UI element that tactile interaction is triggered.
        float TactilePadding { get; }

        //When not using a PinchDetector, the distance in mm that the tip of the thumb and forefinger should be to activate selection during projective interaction.
        float PinchingThreshold { get; }

        InteractionCapability InteractionMode { get; }

        //The distance from the canvas at which to switch to projective mode.
        float ProjectiveToTactileTransitionDistance { get; }

        //The size of the pointer in world coordinates with respect to the distance between the thumb and forefinger.
        AnimationCurve PointerPinchScale { get; }

        //The size of the pointer in world coordinates with respect to the distance between the cursor and the camera.
        AnimationCurve PointerDistanceScale { get; }

        //The Opacity of the Inner Pointer relative to the Primary Pointer.
        float InnerPointerOpacityScalar { get; }

        //Trigger a Hover Event when switching between UI elements.
        bool TriggerHoverOnElementSwitch { get; }

        //The color for the cursor when it is not in a special state.
        Color StandardColor { get; }

        //The color for the cursor when it is hovering over a control.
        Color HoveringColor { get; }

        //The color for the cursor when it is touching or triggering a non-active part of the UI (such as the canvas).
        Color TriggerMissedColor { get; }

        //The color for the cursor when it is actively interacting with a control.
        Color TriggeringColor { get; }
    }
}
