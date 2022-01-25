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
        public ControllerHand LeftHandInputs;
        public ControllerHand RightHandInputs;

        [HideInInspector] public InputMethodType[] CurrentInputTypes;
        private InputMethodType[] _oldInputTypes;

        public Action<Chirality, InputMethodType> OnHandInputTypeChange;
        public Action<Chirality> OnControllerActiveFrame;

        public bool AlwaysEnableControllersIfActive = true;

        /// <summary>
        /// Generates default axis for controllers. 
        /// This fills the Left & Right Hand Inputs with sensible default data. 
        /// </summary>
        [ContextMenu("Generate Default Axis")]
        private void GenerateInputDefaultAxis()
        {
#if ENABLE_INPUT_SYSTEM

            LeftHandInputs = new ControllerHand(Chirality.Left);
            RightHandInputs = new ControllerHand(Chirality.Right);

#else
            Transform lc = null;
            if (LeftHandInputs != null) lc = LeftHandInputs.Transform;

            LeftHandInputs = new ControllerHand(Chirality.Left);
            if (lc != null) LeftHandInputs.Transform = lc;

            Transform rc = null;
            if (RightHandInputs != null) rc = RightHandInputs.Transform;

            RightHandInputs = new ControllerHand(Chirality.Right);
            if (rc != null) RightHandInputs.Transform = rc;
#endif
            LeftHandInputs.GenerateFingers();
            RightHandInputs.GenerateFingers();

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
            if (LeftHandInputs.Transform == null) Debug.LogError("Please assign a left controller.", this);
            if (RightHandInputs.Transform == null) Debug.LogError("Please assign a right controller.", this);
#endif

            LeftHandInputs.Setup(Chirality.Left);
            RightHandInputs.Setup(Chirality.Right);

            if (AlwaysEnableControllersIfActive)
            {
                CurrentInputTypes = new InputMethodType[] { InputMethodType.XRController, InputMethodType.XRController };
            }
            else
            {
                CurrentInputTypes = new InputMethodType[] { InputMethodType.LeapHand, InputMethodType.LeapHand };
            }

            _oldInputTypes = new InputMethodType[CurrentInputTypes.Length];
            CurrentInputTypes.CopyTo(_oldInputTypes, 0);
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

        /// <summary>
        /// Updates input types based on whether a controller is active or not.
        /// If the project is using the Legacy Input System, this does not check for an active controller.
        /// </summary>
        private void UpdateInputTypes()
        {

#if ENABLE_INPUT_SYSTEM
            if (LeftHandInputs.Controller.IsControllerActive())
            {
                OnControllerActiveFrame?.Invoke(Chirality.Left);
                if (AlwaysEnableControllersIfActive)
                {
                    CurrentInputTypes[(int)Chirality.Left] = InputMethodType.XRController;
                }
            }
            else
            {
                CurrentInputTypes[(int)Chirality.Left] = InputMethodType.LeapHand;
            }

            if (RightHandInputs.Controller.IsControllerActive())
            {
                OnControllerActiveFrame?.Invoke(Chirality.Right);
                if (AlwaysEnableControllersIfActive)
                {
                    CurrentInputTypes[(int)Chirality.Right] = InputMethodType.XRController;
                }
            }
            else
            {
                CurrentInputTypes[(int)Chirality.Right] = InputMethodType.LeapHand;
            }
#else
            OnControllerActiveFrame?.Invoke(Chirality.Left);
            OnControllerActiveFrame?.Invoke(Chirality.Right);
#endif

            if (_oldInputTypes[(int)Chirality.Left] != CurrentInputTypes[(int)Chirality.Left])
            {
                _oldInputTypes[(int)Chirality.Left] = CurrentInputTypes[(int)Chirality.Left];
                OnHandInputTypeChange?.Invoke(Chirality.Left, CurrentInputTypes[(int)Chirality.Left]);
            }

            if (_oldInputTypes[(int)Chirality.Right] != CurrentInputTypes[(int)Chirality.Right])
            {
                _oldInputTypes[(int)Chirality.Right] = CurrentInputTypes[(int)Chirality.Right];
                OnHandInputTypeChange?.Invoke(Chirality.Right, CurrentInputTypes[(int)Chirality.Right]);
            }
        }

        public void SetInputMethodType(Chirality chirality, InputMethodType inputMethodType)
        {
            CurrentInputTypes[(int)chirality] = inputMethodType;
        }

        /// <summary>
        /// Updates controller hands to match the controller inputs
        /// </summary>
        private void UpdateControllers()
        {
#if ENABLE_INPUT_SYSTEM
            if (LeftHandInputs.Controller == null)
            {
                LeftHandInputs.Controller = XRController.leftHand;
            }

            if (LeftHandInputs.Controller != null && LeftHandInputs.Controller.wasUpdatedThisFrame)
            {
                LeftHandInputs.Update();
            }

            if (RightHandInputs.Controller == null)
            {
                RightHandInputs.Controller = XRController.rightHand;
            }

            if (RightHandInputs.Controller != null && RightHandInputs.Controller.wasUpdatedThisFrame)
            {
                RightHandInputs.Update();
            }
#else
            LeftHandInputs.Update();
            RightHandInputs.Update();
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
            switch (CurrentInputTypes[(int)chirality])
            {
                case InputMethodType.XRController:
                    inputFrame.Hands.RemoveAll(o => (chirality == Chirality.Left) ? o.IsLeft : o.IsRight);
                    switch (chirality)
                    {
                        case Chirality.Left:
                            inputFrame.Hands.Add(LeftHandInputs.Hand);
                            break;
                        case Chirality.Right:
                            inputFrame.Hands.Add(RightHandInputs.Hand);
                            break;
                    }
                    break;
                case InputMethodType.LeapHand:
                    switch (chirality)
                    {
                        case Chirality.Left:
                            LeftHandInputs.TimeVisible = 0;
                            break;
                        case Chirality.Right:
                            RightHandInputs.TimeVisible = 0;
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
            ControllerHand controllerHand = chirality == Chirality.Left ? LeftHandInputs : RightHandInputs;
            for (int i = 0; i < LeftHandInputs.Fingers[i].Axes.Count; i++)
            {
                try
                {
                    Mathf.Abs(Input.GetAxis(controllerHand.Fingers[i].Axes[i]));
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