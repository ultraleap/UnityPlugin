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
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;

namespace Leap.Unity.Preview.InputActions
{

    #region Leap Bone structure

    // "Leap Bone" is Not currently required as we are not sending full data
    //public struct LeapBone
    //{
    //    public uint parentBoneIndex { get; set; }
    //    public Vector3 position { get; set; }
    //    public Quaternion rotation { get; set; }
    //    public float length { get; set; }
    //    public float width { get; set; }
    //}

    //public class LeapBoneControl : InputControl<LeapBone>
    //{
    //    // The values NEED offsets otherwise the float values will NOT be reported correctly

    //    [InputControl(offset = 0, displayName = "parentBoneIndex")]
    //    public IntegerControl parentBoneIndex { get; private set; }

    //    [InputControl(offset = 4, displayName = "position", noisy = true)]
    //    public Vector3Control position { get; private set; }

    //    [InputControl(offset = 16, displayName = "rotation", noisy = true)]
    //    public QuaternionControl rotation { get; private set; }

    //    [InputControl(offset = 32, displayName = "length", noisy = true)]
    //    public AxisControl length { get; private set; }

    //    [InputControl(offset = 36, displayName = "width", noisy = true)]
    //    public AxisControl width { get; private set; }


    //    protected override void FinishSetup()
    //    {
    //        parentBoneIndex = GetChildControl<IntegerControl>("parentBoneIndex");
    //        position = GetChildControl<Vector3Control>("position");
    //        rotation = GetChildControl<QuaternionControl>("rotation");
    //        length = GetChildControl<AxisControl>("length");
    //        width = GetChildControl<AxisControl>("width");

    //        base.FinishSetup();
    //    }

    //    public override unsafe LeapBone ReadUnprocessedValueFromState(void* statePtr)
    //    {
    //        return new LeapBone()
    //        {
    //            parentBoneIndex = (uint)parentBoneIndex.ReadUnprocessedValueFromState(statePtr),
    //            position = position.ReadUnprocessedValueFromState(statePtr),
    //            rotation = rotation.ReadUnprocessedValueFromState(statePtr),
    //            length = length.ReadUnprocessedValueFromState(statePtr),
    //            width = width.ReadUnprocessedValueFromState(statePtr)
    //        };
    //    }

    //    public override unsafe void WriteValueIntoState(LeapBone value, void* statePtr)
    //    {
    //        parentBoneIndex.WriteValueIntoState((int)value.parentBoneIndex, statePtr);
    //        position.WriteValueIntoState(value.position, statePtr);
    //        rotation.WriteValueIntoState(value.rotation, statePtr);
    //        length.WriteValueIntoState(value.length, statePtr);
    //        width.WriteValueIntoState(value.width, statePtr);
    //    }
    //}

    #endregion

    /// <summary>
    /// The structure used to pass hand data to InputDevices
    /// </summary>
    public struct LeapHandState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('L', 'H', 'N', 'D');

        #region Tracking ID

        [InputControl(name = "Tracked", layout = "Integer")]
        public int tracked;

        #endregion

        #region Palm

        [InputControl(name = "Palm Position", layout = "Vector3")]
        public Vector3 position;

        [InputControl(name = "Palm Direction", layout = "Quaternion")]
        public Quaternion direction;

        #endregion

        #region Index

        [InputControl(name = "Index Tip Position", layout = "Vector3")]
        public Vector3 indexTipPosition;

        #endregion

        #region Extras

        [InputControl(name = "Is Pinching", layout = "Button")]
        public float isPinching;

        [InputControl(name = "Is Grabbing", layout = "Button")]
        public float isGrabbing;

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

        protected override void FinishSetup()
        {
            base.FinishSetup();
        }

        static LeapHandInput()
        {
            Initialize();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // "Leap Bone" is Not currently required as we are not sending full data
            // InputSystem.RegisterLayout<LeapBoneControl>("Leap Bone");

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