/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using System;
using System.Linq;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.XR;
#endif

namespace Leap.Controllers
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

        [HideInInspector] public InputMethodType[] currentInputTypes;
        private InputMethodType[] _oldInputTypes;

        public Action<Chirality, InputMethodType> OnHandInputTypeChange;
        public Action<Chirality> OnControllerActiveFrame;

        [Tooltip("If controllers are active, always use them as our Leap Hand Data.")]
        public bool alwaysEnableControllersIfActive = true;

        /// <summary>
        /// Generates input axes/actions for each finger (e.g. binds the trigger to the index)
        /// Generates sensible default rotation values for the ControllerHand - these can then be customised
        /// </summary>
        [ContextMenu("Generate Default Axis")]
        private void GenerateInputDefaultAxis()
        {
#if ENABLE_INPUT_SYSTEM

            leftHandInputs = new ControllerHand(Chirality.Left);
            rightHandInputs = new ControllerHand(Chirality.Right);

#else
            Transform lc = leftHandInputs?.transform;

            leftHandInputs = new ControllerHand(Chirality.Left);
            if (lc != null) leftHandInputs.transform = lc;

            Transform rc = rightHandInputs?.transform;

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
            if (LegacyXRInputBindingsNotSeeded())
            {
                gameObject.SetActive(false);
                passthroughOnly = true;
                Debug.LogError("The controller post processor is reliant on the" +
                            " XR Legacy Input Helpers package when using the Legacy Input Module. Please add this package to your project" +
                            "and Seed XR Input Bindings.");
            }
            if (leftHandInputs.transform == null) Debug.LogError("Please assign a left controller to Left Hand Inputs -> Transform.", this);
            if (rightHandInputs.transform == null) Debug.LogError("Please assign a right controller to Right Hand Inputs -> Transform", this);
#endif

            leftHandInputs.Setup(Chirality.Left);
            rightHandInputs.Setup(Chirality.Right);

            if (alwaysEnableControllersIfActive)
            {
                currentInputTypes = new InputMethodType[] { InputMethodType.XRController, InputMethodType.XRController };
            }
            else
            {
                currentInputTypes = new InputMethodType[] { InputMethodType.LeapHand, InputMethodType.LeapHand };
            }

            _oldInputTypes = currentInputTypes.ToArray();
        }

        private void Update()
        {
            UpdateInputTypes();
            UpdateControllers();
        }

        private void Reset()
        {
            GenerateInputDefaultAxis();
        }

        public InputMethodType GetCurrentInputMethodTypeByChirality(Chirality chirality)
        {
            return currentInputTypes[(int)chirality];
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
                OnControllerActiveFrame?.Invoke(Chirality.Left);
                if (alwaysEnableControllersIfActive)
                {
                    currentInputTypes[(int)Chirality.Left] = InputMethodType.XRController;
                }
            }
            else
            {
                currentInputTypes[(int)Chirality.Left] = InputMethodType.LeapHand;
            }

            if (rightHandInputs.controller.IsControllerActive())
            {
                OnControllerActiveFrame?.Invoke(Chirality.Right);
                if (alwaysEnableControllersIfActive)
                {
                    currentInputTypes[(int)Chirality.Right] = InputMethodType.XRController;
                }
            }
            else
            {
                currentInputTypes[(int)Chirality.Right] = InputMethodType.LeapHand;
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
                            inputFrame.Hands.Add(leftHandInputs.Hand);
                            break;
                        case Chirality.Right:
                            inputFrame.Hands.Add(rightHandInputs.Hand);
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

        private bool LegacyXRInputBindingsNotSeeded()
        {
            bool leftNotSeeded = LegacyXRInputBindingsNotSeeded(Chirality.Left);
            bool rightNotSeeded = LegacyXRInputBindingsNotSeeded(Chirality.Right);

            return leftNotSeeded || rightNotSeeded;
        }

        private bool LegacyXRInputBindingsNotSeeded(Chirality chirality)
        {
            ControllerHand controllerHand = chirality == Chirality.Left ? leftHandInputs : rightHandInputs;
            for (int i = 0; i < leftHandInputs.fingers[i].axes.Count; i++)
            {
                try
                {
                    Mathf.Abs(Input.GetAxis(controllerHand.fingers[i].axes[i]));
                }
                catch
                {
                    return true;
                }
            }
            return false;
        }
    }
}