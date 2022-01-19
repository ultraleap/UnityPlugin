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
    public class HandControllerSwapper : MonoBehaviour
    {
        public ControllerPostProcess controllerPostProcess;
        public ControllerProfile ControllerProfile = new ControllerProfile();
        private ControllerProfile _oldProfile;
        private InputMethodType[] _inputType;

        private LeapProvider _originalProvider;

        private void Start()
        {
            Setup(controllerPostProcess.inputLeapProvider, controllerPostProcess.currentInputTypes);
            controllerPostProcess.OnControllerActiveFrame += SetCurrentInputType;
            controllerPostProcess.AlwaysEnableControllersIfActive = false;
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

        public void SetCurrentInputType(Chirality chirality)
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

        public bool IsInputGroupValid(List<ControllerProfile.HandChecks> inputs)
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

        public bool IsStageValid(ControllerProfile.HandChecks stage)
        {
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

        private void OnValidate()
        {
            if (Application.isPlaying && ControllerProfile != _oldProfile)
            {
                ControllerProfile.SetupProfiles(_originalProvider);
                _oldProfile = ControllerProfile;
            }
        }
    }
}