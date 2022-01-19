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
        public ControllerPostProcess controllerPostProcess;
        public ControllerProfile ControllerProfile = new ControllerProfile();
        private InputMethodType[] _inputType;

        private LeapProvider _originalProvider;

        private Vector3 leftControllerVelocity, rightControllerVelocity;
        private Vector3 prevLeftControllerPosition, prevRightControllerPosition;

        private void Start()
        {
            Setup(controllerPostProcess.inputLeapProvider, controllerPostProcess.currentInputTypes);
            controllerPostProcess.OnControllerActiveFrame += SetCurrentInputType;
            controllerPostProcess.AlwaysEnableControllersIfActive = false;
            leftControllerVelocity = Vector3.zero;
            rightControllerVelocity = Vector3.zero;
            prevLeftControllerPosition = Vector3.zero;
            prevRightControllerPosition = Vector3.zero;
        }

        private void Update()
        {
            Vector3 currentLeftControllerPos = controllerPostProcess.leftHandInputs.transform.position;
            leftControllerVelocity = (currentLeftControllerPos - prevLeftControllerPosition) / Time.deltaTime;
            prevLeftControllerPosition = currentLeftControllerPos;

            Vector3 currentRightControllerPos = controllerPostProcess.rightHandInputs.transform.position;
            rightControllerVelocity = (currentRightControllerPos - prevRightControllerPosition) / Time.deltaTime;
            prevRightControllerPosition = currentRightControllerPos;
        }

        public void Setup(LeapProvider originalProvider, InputMethodType[] initialInputMethodTypes)
        {
            _originalProvider = originalProvider;
            _inputType = initialInputMethodTypes;

            if (ControllerProfile == null)
            {
                ControllerProfile = new ControllerProfile();
            }

            ControllerProfile.SetupProfiles(_originalProvider);
        }

        /// <summary>
        /// This is the heart of the HandControllerSwapper. It takes in a chirality and checks both the Leap Hand 
        /// and the XR Controller inputs of that chirality, working out which is most appropriate to use at the given time.
        /// </summary>
        /// <param name="chirality"></param>
        private void SetCurrentInputType(Chirality chirality)
        {
            if (ControllerProfile == null)
            {
                controllerPostProcess.SetInputMethodType(chirality, InputMethodType.LeapHand);
                return;
            }

            switch (chirality)
            {
                case Chirality.Left:
                    if (IsInputGroupValid(_inputType[(int)chirality] == InputMethodType.LeapHand ? ControllerProfile.leftHandChecks : ControllerProfile.leftControllerChecks))
                    {
                        if (_inputType[(int)chirality] == InputMethodType.LeapHand)
                        {
                            controllerPostProcess.SetInputMethodType(chirality, InputMethodType.XRController);
                        }
                        else
                        {
                            controllerPostProcess.SetInputMethodType(chirality, InputMethodType.LeapHand);
                        }
                        return;
                    }
                    break;

                case Chirality.Right:
                    if (IsInputGroupValid(_inputType[(int)chirality] == InputMethodType.LeapHand ? ControllerProfile.rightHandChecks : ControllerProfile.rightControllerChecks))
                    {
                        if (_inputType[(int)chirality] == InputMethodType.LeapHand)
                        {
                            controllerPostProcess.SetInputMethodType(chirality, InputMethodType.XRController);
                        }
                        else
                        {
                            controllerPostProcess.SetInputMethodType(chirality, InputMethodType.LeapHand);
                        }
                        return;
                    }
                    break;
            }

            controllerPostProcess.SetInputMethodType(chirality, _inputType[(int)chirality]);
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
            List<InputCheckBase> mandatoryElements = stage.checks.FindAll(x => x.mandatory);
            bool mandatory = true;
            List<InputCheckBase> notMandatoryElements = stage.checks.FindAll(x => !x.mandatory);
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

        private void UpdateLegacyInputSystemVariables(ref ControllerProfile.InputCheckStage stage)
        {
            stage.checks.FindAll(check => check is InputVelocity)
                .ForEach(check =>
                {
                    InputVelocity inputVelocityCheck = check as InputVelocity;
                    inputVelocityCheck.currentVelocity = inputVelocityCheck.hand == Chirality.Left ?
                    leftControllerVelocity : 
                    rightControllerVelocity;
                }
            );

            stage.checks.FindAll(check => check is DistanceFromHead)
                .ForEach(check =>
                {
                    DistanceFromHead distanceFromHeadCheck = check as DistanceFromHead;
                    distanceFromHeadCheck.currentXRControllerPosition = distanceFromHeadCheck.hand == Chirality.Left ? 
                    controllerPostProcess.leftHandInputs.transform.position :
                    controllerPostProcess.rightHandInputs.transform.position;
                }
            );
            
            stage.checks.FindAll(check => check is DistanceBetweenInputs)
                .ForEach(check =>
                {
                    DistanceBetweenInputs distanceBetweenInputsCheck = check as DistanceBetweenInputs;
                    distanceBetweenInputsCheck.currentXRControllerPosition = distanceBetweenInputsCheck.hand == Chirality.Left ? 
                    controllerPostProcess.leftHandInputs.transform.position :
                    controllerPostProcess.rightHandInputs.transform.position;
                }
            );
            
            stage.checks.FindAll(check => check is IsFacingDown)
                .ForEach(check =>
                {
                    IsFacingDown isFacingDownCheck = check as IsFacingDown;
                    isFacingDownCheck.currentXRControllerRotation = isFacingDownCheck.hand == Chirality.Left ? 
                    controllerPostProcess.leftHandInputs.transform.rotation :
                    controllerPostProcess.rightHandInputs.transform.rotation;
                }
            );
        }
    }
}