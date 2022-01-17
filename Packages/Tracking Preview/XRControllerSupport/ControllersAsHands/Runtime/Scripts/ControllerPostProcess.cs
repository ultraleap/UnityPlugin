/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using UnityEngine;
using System;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.XR;
#endif

namespace Leap.Unity.Controllers
{
    public enum InputMethodType
    {
        LeapHand,
        XRController,
    }

    /// <summary>
    /// Controller Post Process takes in XR Controller data and transforms it into 
    /// Leap Hand data, represented by ControllerHand.
    /// </summary>
    public class ControllerPostProcess : PostProcessProvider
    {
        public ControllerHand leftHandInputs;
        public ControllerHand rightHandInputs;

        public InputMethodType[] currentInputTypes;
        private InputMethodType[] _oldInputTypes;

        public Action<Chirality, InputMethodType> OnHandInputTypeChange;
        public Action<Chirality> OnControllerActiveFrame;

        /// <summary>
        /// Generates default axis for controllers. 
        /// This fills the Left & Right Hand Inputs with sensible default data. 
        /// </summary>
        [ContextMenu("Generate Default Axis")]
        private void GenerateInputDefaultAxis()
        {
#if ENABLE_INPUT_SYSTEM

            leftHandInputs = new ControllerHand(Chirality.Left);
            rightHandInputs = new ControllerHand(Chirality.Right);

#else
        Transform lc = null;
        if (leftHandInputs != null) lc = leftHandInputs.transform;
        
        leftHandInputs = new ControllerHand(Chirality.Left);
        if (lc != null) leftHandInputs.transform = lc;

        Transform rc = null;
        if (rightHandInputs != null) rc = rightHandInputs.transform;

        rightHandInputs = new ControllerHand(Chirality.Right);
        if (rc != null) rightHandInputs.transform = rc;
#endif
            leftHandInputs.GenerateFingers();
            rightHandInputs.GenerateFingers();

        }

        private void Awake()
        {
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
        Debug.LogWarning("Both the Input System & Legacy Input system are enabled at the same time. This may result in undesired behaviour."
        +"ControllerPostProcess will default to using the Input System.");
#endif

#if !ENABLE_INPUT_SYSTEM
        if (leftHandInputs.transform == null) Debug.LogError("Please assign a left controller.", this);
        if (rightHandInputs.transform == null) Debug.LogError("Please assign a right controller.", this);
#endif

            leftHandInputs.Setup(Chirality.Left);
            rightHandInputs.Setup(Chirality.Right);

            currentInputTypes = new InputMethodType[] { InputMethodType.XRController, InputMethodType.XRController };

            _oldInputTypes = new InputMethodType[currentInputTypes.Length];
            currentInputTypes.CopyTo(_oldInputTypes, 0);
        }

        private void Update()
        {
            UpdateInputTypes();
            UpdateControllers();
        }

        /// <summary>
        /// Updates input types based on whether a controller is active or not.
        /// If the project is using the Legacy Input System, this does not check for an active controller.
        /// </summary>
        private void UpdateInputTypes()
        {
#if ENABLE_INPUT_SYSTEM

            if (leftHandInputs.controller.IsControllerActive())
            {
                currentInputTypes[(int)Chirality.Left] = InputMethodType.XRController;
            }
            else
            {
                currentInputTypes[(int)Chirality.Left] = InputMethodType.LeapHand;
            }

            if (rightHandInputs.controller.IsControllerActive())
            {
                currentInputTypes[(int)Chirality.Right] = InputMethodType.XRController;
            }
            else
            {
                currentInputTypes[(int)Chirality.Right] = InputMethodType.LeapHand;
            }
#endif

#if ENABLE_INPUT_SYSTEM
            if (leftHandInputs.controller.IsControllerActive())
            {
                OnControllerActiveFrame?.Invoke(Chirality.Left);
            }

            if (rightHandInputs.controller.IsControllerActive())
            {
                OnControllerActiveFrame?.Invoke(Chirality.Right);
            }
#else
            OnControllerActiveFrame?.Invoke(Chirality.Left);
            OnControllerActiveFrame?.Invoke(Chirality.Right);
#endif

            if (_oldInputTypes[(int)Chirality.Left] != currentInputTypes[(int)Chirality.Left])
            {
                _oldInputTypes[(int)Chirality.Left] = currentInputTypes[(int)Chirality.Left];
                OnHandInputTypeChange?.Invoke(Chirality.Left, currentInputTypes[(int)Chirality.Left]);
            }

            if (_oldInputTypes[(int)Chirality.Right] != currentInputTypes[(int)Chirality.Right])
            {
                _oldInputTypes[(int)Chirality.Right] = currentInputTypes[(int)Chirality.Right];
                OnHandInputTypeChange?.Invoke(Chirality.Right, currentInputTypes[(int)Chirality.Right]);
            }
        }

        public void SetInputMethodType(Chirality chirality, InputMethodType inputMethodType)
        {
            currentInputTypes[(int)chirality] = inputMethodType;
        }

        /// <summary>
        /// Updates controller hands to match the controller inputs
        /// </summary>
        private void UpdateControllers()
        {
#if ENABLE_INPUT_SYSTEM
            if (leftHandInputs.controller == null)
            {
                leftHandInputs.controller = XRController.leftHand;
            }

            if (leftHandInputs.controller != null && leftHandInputs.controller.wasUpdatedThisFrame)
            {
                leftHandInputs.Update();
            }

            if (rightHandInputs.controller == null)
            {
                rightHandInputs.controller = XRController.rightHand;
            }

            if (rightHandInputs.controller != null && rightHandInputs.controller.wasUpdatedThisFrame)
            {
                rightHandInputs.Update();
            }
#else
            leftHandInputs.Update();
            rightHandInputs.Update();
#endif
        }

        public override void ProcessFrame(ref Frame inputFrame)
        {
            if (!Application.isPlaying) return;

            ProcessHandData(Chirality.Left, ref inputFrame);
            ProcessHandData(Chirality.Right, ref inputFrame);
        }

        /// <summary>
        /// Adds controller hand data to the leap frame, if the InputMethodType is set to XRController
        /// </summary>
        /// <param name="chirality"></param>
        /// <param name="inputFrame"></param>
        private void ProcessHandData(Chirality chirality, ref Frame inputFrame)
        {
            switch (currentInputTypes[(int)chirality])
            {
                case InputMethodType.XRController:
                    inputFrame.Hands.RemoveAll(o => (chirality == Chirality.Left) ? o.IsLeft : o.IsRight);
                    switch (chirality)
                    {
                        case Chirality.Left:
                            inputFrame.Hands.Add(leftHandInputs.hand);
                            break;
                        case Chirality.Right:
                            inputFrame.Hands.Add(rightHandInputs.hand);
                            break;
                    }
                    break;
                case InputMethodType.LeapHand:
                    switch (chirality)
                    {
                        case Chirality.Left:
                            leftHandInputs.timeVisible = 0;
                            break;
                        case Chirality.Right:
                            rightHandInputs.timeVisible = 0;
                            break;
                    }
                    break;
            }
        }
    }
}