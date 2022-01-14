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

    public class ControllerPostProcess : PostProcessProvider
    {
        public ControllerHand leftHandInputs;
        public ControllerHand rightHandInputs;

        [Tooltip("If enabled, input will default to controllers")]
        public bool DefaultToControllers = false;

        public InputMethodType[] currentInputTypes;
        private InputMethodType[] _oldInputTypes;

        public Action<Chirality, InputMethodType> OnHandInputTypeChange;
        public Action<Chirality> OnControllerActiveFrame;

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
            leftHandInputs.GenerateLimbs();
            rightHandInputs.GenerateLimbs();

        }

        private void Awake()
        {
#if !ENABLE_INPUT_SYSTEM

        if (leftHandInputs.transform == null) Debug.LogError("Please assign a left controller.", this);
        if (rightHandInputs.transform == null) Debug.LogError("Please assign a right controller.", this);
#endif

            leftHandInputs.Setup(Chirality.Left);
            rightHandInputs.Setup(Chirality.Right);

            if (DefaultToControllers)
            {
                currentInputTypes = new InputMethodType[] { InputMethodType.XRController, InputMethodType.XRController };
            }
            else
            {
                currentInputTypes = new InputMethodType[] { InputMethodType.LeapHand, InputMethodType.LeapHand };
            }

            _oldInputTypes = new InputMethodType[currentInputTypes.Length];
            currentInputTypes.CopyTo(_oldInputTypes, 0);
        }

        private void Update()
        {
            UpdateInputTypes();
            UpdateControllers();
        }

        private void UpdateInputTypes()
        {
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


        private void UpdateControllers()
        {
#if ENABLE_INPUT_SYSTEM
            if (leftHandInputs.controller == null)
            {
                leftHandInputs.controller = XRController.leftHand;
            }

            if (leftHandInputs.controller != null && leftHandInputs.controller.wasUpdatedThisFrame)
#endif
                leftHandInputs.Update();

#if ENABLE_INPUT_SYSTEM
            if (rightHandInputs.controller == null)
            {
                rightHandInputs.controller = XRController.rightHand;
            }

            if (rightHandInputs.controller != null && rightHandInputs.controller.wasUpdatedThisFrame)
#endif
                rightHandInputs.Update();
        }

        public override void ProcessFrame(ref Frame inputFrame)
        {
            if (!Application.isPlaying) return;

            ProcessHandData(Chirality.Left, ref inputFrame);
            ProcessHandData(Chirality.Right, ref inputFrame);
        }

        private void ProcessHandData(Chirality chirality, ref Frame inputFrame)
        {

#if ENABLE_INPUT_SYSTEM
            switch (chirality)
            {
                case Chirality.Left:
                    if (leftHandInputs.controller.IsControllerActive())
                    {
                        currentInputTypes[(int)chirality] = InputMethodType.XRController;
                    }
                    else
                    {
                        currentInputTypes[(int)chirality] = InputMethodType.LeapHand;
                    }
                    break;
                case Chirality.Right:
                    if (rightHandInputs.controller.IsControllerActive())
                    {
                        currentInputTypes[(int)chirality] = InputMethodType.XRController;
                    }
                    else
                    {
                        currentInputTypes[(int)chirality] = InputMethodType.LeapHand;
                    }
                    break;
            }
#endif

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