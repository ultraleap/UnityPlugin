/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Leap;
using Leap.Unity;
using UnityEngine;
using System.Reflection;

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
            Setup(_controllerPostProcess.inputLeapProvider, _controllerPostProcess.CurrentInputTypes);
            _controllerPostProcess.OnControllerActiveFrame += SetCurrentInputType;
            _controllerPostProcess.AlwaysEnableControllersIfActive = false;
        }

        private void Update()
        {
#if !ENABLE_INPUT_SYSTEM
            Vector3 currentLeftControllerPos = _controllerPostProcess.LeftHandInputs.Transform.position;
            _leftControllerVelocity = (currentLeftControllerPos - _prevLeftControllerPosition) / Time.deltaTime;
            _prevLeftControllerPosition = currentLeftControllerPos;

            Vector3 currentRightControllerPos = _controllerPostProcess.RightHandInputs.Transform.position;
            _rightControllerVelocity = (currentRightControllerPos - _prevRightControllerPosition) / Time.deltaTime;
            _prevRightControllerPosition = currentRightControllerPos;
#endif
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
                    if (IsInputGroupValid(_inputType[(int)chirality] == InputMethodType.LeapHand ? _controllerProfile.LeftHandChecks : _controllerProfile.LeftControllerChecks))
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
                    if (IsInputGroupValid(_inputType[(int)chirality] == InputMethodType.LeapHand ? _controllerProfile.RightHandChecks : _controllerProfile.RightControllerChecks))
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
                inputs[i].checks.ForEach(x => x.Reset());
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
            List<InputCheckBase> mandatoryElements = stage.checks.FindAll(x => x.Mandatory);
            bool mandatory = true;
            List<InputCheckBase> notMandatoryElements = stage.checks.FindAll(x => !x.Mandatory);
            bool notMandatory = true;
            for (int i = 0; i < mandatoryElements.Count; i++)
            {
                if (!mandatoryElements[i].IsTrue())
                {
                    mandatory = false;
                }
            }
            int count = 0;
            for (int i = 0; i < notMandatoryElements.Count; i++)
            {
                if (notMandatoryElements[i].IsTrue())
                {
                    count++;
                }
            }
            notMandatory = count > 0;
            return mandatory && notMandatory;
        }

#if !ENABLE_INPUT_SYSTEM
        private void UpdateLegacyInputSystemVariables(ref ControllerProfile.InputCheckStage stage)
        {
            stage.checks.FindAll(check => check is InputVelocity)
                .ForEach(check =>
                {
                    InputVelocity inputVelocityCheck = check as InputVelocity;
                    inputVelocityCheck.CurrentVelocity = inputVelocityCheck.Hand == Chirality.Left ?
                    _leftControllerVelocity :
                    _rightControllerVelocity;
                }
            );

            stage.checks.FindAll(check => check is DistanceFromHead)
                .ForEach(check =>
                {
                    DistanceFromHead distanceFromHeadCheck = check as DistanceFromHead;
                    distanceFromHeadCheck.CurrentXRControllerPosition = distanceFromHeadCheck.Hand == Chirality.Left ?
                    _controllerPostProcess.LeftHandInputs.Transform.position :
                    _controllerPostProcess.RightHandInputs.Transform.position;
                }
            );

            stage.checks.FindAll(check => check is DistanceBetweenInputs)
                .ForEach(check =>
                {
                    DistanceBetweenInputs distanceBetweenInputsCheck = check as DistanceBetweenInputs;
                    distanceBetweenInputsCheck.CurrentXRControllerPosition = distanceBetweenInputsCheck.Hand == Chirality.Left ?
                    _controllerPostProcess.LeftHandInputs.Transform.position :
                    _controllerPostProcess.RightHandInputs.Transform.position;
                }
            );

            stage.checks.FindAll(check => check is IsFacingDown)
                .ForEach(check =>
                {
                    IsFacingDown isFacingDownCheck = check as IsFacingDown;
                    isFacingDownCheck.CurrentXRControllerRotation = isFacingDownCheck.Hand == Chirality.Left ?
                    _controllerPostProcess.LeftHandInputs.Transform.rotation :
                    _controllerPostProcess.RightHandInputs.Transform.rotation;
                }
            );
        }
#endif
    }
}