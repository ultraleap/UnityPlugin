/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace Leap.Unity.InputActions
{
    /// <summary>
    /// The structure used to pass hand data to InputDevices
    /// </summary>
    public struct LeapHandState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('L', 'H', 'N', 'D');

        #region Tracking

        [InputControl(name = "Tracked", layout = "Integer")]
        public int tracked;

        #endregion

        #region Actions

        [InputControl(name = "Selecting", layout = "Button")]
        public float selecting;

        [InputControl(name = "Activating", layout = "Button")]
        public float activating;

        #endregion

        #region Positions and Directions

        [InputControl(name = "Palm Position", layout = "Vector3")]
        public Vector3 palmPosition;

        [InputControl(name = "Palm Direction", layout = "Quaternion")]
        public Quaternion palmDirection;

        [InputControl(name = "Aim Position", layout = "Vector3")]
        public Vector3 aimPosition;

        [InputControl(name = "Aim Direction", layout = "Quaternion")]
        public Quaternion aimDirection;

        [InputControl(name = "Pinch Position", layout = "Vector3")]
        public Vector3 pinchPosition;

        [InputControl(name = "Pinch Direction", layout = "Quaternion")]
        public Quaternion pinchDirection;

        [InputControl(name = "Poke Position", layout = "Vector3")]
        public Vector3 pokePosition;

        [InputControl(name = "Poke Direction", layout = "Quaternion")]
        public Quaternion pokeDirection;

        #endregion
    }
    /// <summary>
    /// An Input Device generated using two individual hand Input Devices representing left and right hands.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [InputControlLayout(displayName = "Ultraleap", stateType = typeof(LeapHandState), commonUsages = new[] { "LeftHand", "RightHand" }, canRunInBackground = true, isNoisy = true)]
    public class LeapHandInput : InputDevice
    {
        public static LeapHandInput leftHand => InputSystem.GetDevice<LeapHandInput>(CommonUsages.LeftHand);
        public static LeapHandInput rightHand => InputSystem.GetDevice<LeapHandInput>(CommonUsages.RightHand);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            UltraleapSettings ultraleapSettings = UltraleapSettings.Instance;

            if (ultraleapSettings == null ||
                ultraleapSettings.updateLeapInputSystem == false)
            {
                return;
            }

            // RegisterLayout() adds a "Control layout" to the system.
            // These can be layouts for individual Controls (like sticks)
            // or layouts for entire Devices (which are themselves
            // Controls) like in our case.
            InputSystem.RegisterLayout<LeapHandInput>(name: "Leap Hand",
                matches: new InputDeviceMatcher()
                .WithManufacturer("Ultraleap")
                .WithProduct("^(Leap Hand)")
                );
        }
    }
}