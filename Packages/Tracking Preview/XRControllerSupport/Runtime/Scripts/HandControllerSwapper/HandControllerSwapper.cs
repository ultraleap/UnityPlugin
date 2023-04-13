/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Controllers
{
    /// <summary>
    /// HandControllerSwapper is used to swap between controller input and hand input.
    /// </summary>
    public class HandControllerSwapper : MonoBehaviour
    {

        [Tooltip("Hand ControllerSwapper will set Input Method Types on the Controller Post Process referenced here")]
        [SerializeField] private ControllerPostProcess _controllerPostProcess;

        private ControllerProfile _controllerProfile = new ControllerProfile();
        private InputMethodType[] _inputType;
        private LeapProvider _originalProvider;

#if !ENABLE_INPUT_SYSTEM
        private Vector3 _leftControllerVelocity = Vector3.zero, _rightControllerVelocity = Vector3.zero;
        private Vector3 _prevLeftControllerPosition = Vector3.zero, _prevRightControllerPosition = Vector3.zero;
#endif

        private void Start()
        {
            Setup(_controllerPostProcess.inputLeapProvider, _controllerPostProcess.currentInputTypes);
            _controllerPostProcess.alwaysEnableControllersIfActive = false;
        }

        private void Update()
        {
#if !ENABLE_INPUT_SYSTEM
            Vector3 currentLeftControllerPos = _controllerPostProcess.leftHandInputs.transform.position;
            _leftControllerVelocity = (currentLeftControllerPos - _prevLeftControllerPosition) / Time.deltaTime;
            _prevLeftControllerPosition = currentLeftControllerPos;

            Vector3 currentRightControllerPos = _controllerPostProcess.rightHandInputs.transform.position;
            _rightControllerVelocity = (currentRightControllerPos - _prevRightControllerPosition) / Time.deltaTime;
            _prevRightControllerPosition = currentRightControllerPos;
#endif
        }

        private void OnEnable()
        {
            _controllerPostProcess.OnControllerActiveFrame += SetCurrentInputType;
        }

        private void OnDisable()
        {
            _controllerPostProcess.OnControllerActiveFrame -= SetCurrentInputType;
        }

        public void Setup(LeapProvider originalProvider, InputMethodType[] initialInputMethodTypes)
        {
            _originalProvider = originalProvider;
            _inputType = initialInputMethodTypes;

            if (_controllerProfile == null)
            {
                _controllerProfile = new ControllerProfile();
            }

            _controllerProfile.SetupProfiles(_originalProvider);
        }

        /// <summary>
        /// This is the heart of the HandControllerSwapper. It takes in a chirality and checks both the Leap Hand 
        /// and the XR Controller inputs of that chirality, working out which is most appropriate to use at the given time.
        /// </summary>
        /// <param name="chirality"></param>
        private void SetCurrentInputType(Chirality chirality)
        {
            if (_controllerProfile == null)
            {
                _controllerPostProcess.SetInputMethodType(chirality, InputMethodType.LeapHand);
                return;
            }

            switch (chirality)
            {
                case Chirality.Left:
                    if (IsInputGroupValid(_inputType[(int)chirality] == InputMethodType.LeapHand ? _controllerProfile.leftHandChecks : _controllerProfile.leftControllerChecks))
                    {
                        if (_inputType[(int)chirality] == InputMethodType.LeapHand)
                        {
                            _controllerPostProcess.SetInputMethodType(chirality, InputMethodType.XRController);
                        }
                        else
                        {
                            _controllerPostProcess.SetInputMethodType(chirality, InputMethodType.LeapHand);
                        }
                        return;
                    }
                    break;

                case Chirality.Right:
                    if (IsInputGroupValid(_inputType[(int)chirality] == InputMethodType.LeapHand ? _controllerProfile.rightHandChecks : _controllerProfile.rightControllerChecks))
                    {
                        if (_inputType[(int)chirality] == InputMethodType.LeapHand)
                        {
                            _controllerPostProcess.SetInputMethodType(chirality, InputMethodType.XRController);
                        }
                        else
                        {
                            _controllerPostProcess.SetInputMethodType(chirality, InputMethodType.LeapHand);
                        }
                        return;
                    }
                    break;
            }

            _controllerPostProcess.SetInputMethodType(chirality, _inputType[(int)chirality]);
        }

        /// <summary>
        /// This checks that an InputGroup is valid.
        /// It does this by looping through all the InputCheckStages passed in through inputs.
        /// If all stages are valid, then the InputGroup is valid, and we return true.
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public bool IsInputGroupValid(List<ControllerProfile.InputCheckStage> inputs)
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                if (!IsStageValid(inputs[i]))
                {
                    return false;
                }
            }
            for (int i = 0; i < inputs.Count; i++)
            {
                inputs[i].Checks.ForEach(x => x.Reset());
            }
            return true;
        }

        /// <summary>
        /// This loops through all InputChecks in an InputCheckStage
        /// InputCheckStages are a group of InputChecks.
        /// If one InputCheck in an InputCheckStage is true, the stage is classed as valid.
        /// Some InputChecks can be classed as mandatory - if this is the case, 
        /// these must be true for the stage to pass.
        /// </summary>
        /// <param name="stage"></param>
        /// <returns></returns>
        public bool IsStageValid(ControllerProfile.InputCheckStage stage)
        {
#if !ENABLE_INPUT_SYSTEM
            UpdateLegacyInputSystemVariables(ref stage);
#endif
            for (int i = 0; i < stage.Checks.Count; i++)
            {
                if (stage.Checks[i].IsTrue())
                {
                    return true;
                }
            }
            return false;
        }

#if !ENABLE_INPUT_SYSTEM
        private void UpdateLegacyInputSystemVariables(ref ControllerProfile.InputCheckStage stage)
        {
            stage.Checks.FindAll(check => check is InputVelocity)
                .ForEach(check =>
                {
                    InputVelocity inputVelocityCheck = check as InputVelocity;
                    inputVelocityCheck.currentVelocity = inputVelocityCheck.hand == Chirality.Left ?
                    _leftControllerVelocity :
                    _rightControllerVelocity;
                }
            );

            stage.Checks.FindAll(check => check is DistanceFromHead)
                .ForEach(check =>
                {
                    DistanceFromHead distanceFromHeadCheck = check as DistanceFromHead;
                    distanceFromHeadCheck.currentXRControllerPosition = distanceFromHeadCheck.hand == Chirality.Left ?
                    _controllerPostProcess.leftHandInputs.transform.position :
                    _controllerPostProcess.rightHandInputs.transform.position;
                }
            );

            stage.Checks.FindAll(check => check is DistanceBetweenInputs)
                .ForEach(check =>
                {
                    DistanceBetweenInputs distanceBetweenInputsCheck = check as DistanceBetweenInputs;
                    distanceBetweenInputsCheck.currentXRControllerPosition = distanceBetweenInputsCheck.hand == Chirality.Left ?
                    _controllerPostProcess.leftHandInputs.transform.position :
                    _controllerPostProcess.rightHandInputs.transform.position;
                }
            );

            stage.Checks.FindAll(check => check is IsFacingDown)
                .ForEach(check =>
                {
                    IsFacingDown isFacingDownCheck = check as IsFacingDown;
                    isFacingDownCheck.currentXRControllerRotation = isFacingDownCheck.hand == Chirality.Left ?
                    _controllerPostProcess.leftHandInputs.transform.rotation :
                    _controllerPostProcess.rightHandInputs.transform.rotation;
                }
            );
        }
#endif
    }
}