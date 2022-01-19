/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

namespace Leap.Unity.Controllers
{

    /// <summary>
    /// ControllerProfile sets up and stores the various InputChecks and is primarily used
    /// as a data object
    /// </summary>
    public class ControllerProfile 
    {
        public List<InputCheckStage> leftHandChecks = new List<InputCheckStage>();
        public List<InputCheckStage> rightHandChecks = new List<InputCheckStage>();

        public List<InputCheckStage> leftControllerChecks = new List<InputCheckStage>();
        public List<InputCheckStage> rightControllerChecks = new List<InputCheckStage>();

        public ControllerProfile()
        {
            PopulateInputCheckStages();
        }

        public class InputCheckStage
        {
            public List<InputCheckBase> checks = new List<InputCheckBase>();
            
            public InputCheckStage(List<InputCheckBase> checks)
            {
                this.checks = checks;
            }

            public void SetInputCheckChirality(Chirality chirality)
            {
                checks.ForEach(check => check.hand = chirality);
            }

        }

        public void SetupProfiles(LeapProvider originalProvider)
        {
            SetupProfiles(leftHandChecks, originalProvider);
            SetupProfiles(rightHandChecks, originalProvider);
            SetupProfiles(leftControllerChecks, originalProvider);
            SetupProfiles(rightControllerChecks, originalProvider);
        }

        private void SetupProfiles(List<InputCheckStage> checks, LeapProvider provider)
        {
            for (int i = 0; i < checks.Count; i++)
            {
                for (int j = 0; j < checks[i].checks.Count; j++)
                {
                    checks[i].checks[j].Setup(provider);
                }
            }
        }

        private void PopulateInputCheckStages()
        {
            List<InputCheckBase> handToControllerChecksStage0 = new List<InputCheckBase>()
            {
                new DistanceBetweenInputs()
                {
                    inputMethodType = InputMethodType.LeapHand,
                    actionThreshold = 0.12f
                },
                new HasButtonBeenPressed()
                {
                    inputMethodType = InputMethodType.XRController,
                },
                new InputIsInactive()
                {
                    inputMethodType = InputMethodType.LeapHand,
                },
            };

            List<InputCheckBase> handToControllerChecksStage1 = new List<InputCheckBase>()
            {
               new DistanceBetweenInputs()
               {
                    inputMethodType = InputMethodType.LeapHand,
                    actionThreshold = 0.12f,
                    useTime = true,
                    timeThreshold = 2,
               },
               new HasButtonBeenPressed()
               {
                    inputMethodType = InputMethodType.XRController,
               },
               new InputVelocity()
               {
                    inputMethodType = InputMethodType.XRController,
                    velocityIsLower = false,
                    actionThreshold = 0.015f,
                    useTime = true,
                    timeThreshold = 0.2f
               }
            };

            List<InputCheckBase> controllerToHandChecksStage0 = new List<InputCheckBase>()
            {
                new DistanceFromHead()
                {
                    inputMethodType = InputMethodType.XRController,
                    lessThan = false,
                    actionThreshold = 1.1f,
                    useTime = true,
                    timeThreshold = 0.1f
                },
                new InputVelocity()
                {
                    inputMethodType = InputMethodType.XRController,
                    velocityIsLower = true,
                    actionThreshold = 0.01f,
                    useTime = true,
                    timeThreshold = 0.2f,
                },
                new IsFacingDown()
                {
                    inputMethodType = InputMethodType.XRController,
                    useTime = true,
                    actionThreshold = 30,
                    timeThreshold = 0.2f
                }
            };

            List<InputCheckBase> controllerToHandChecksStage1 = new List<InputCheckBase>()
            {
                new DistanceBetweenInputs()
                {
                    inputMethodType = InputMethodType.XRController,
                    actionThreshold = 0.2f
                },
            };

            List<InputCheckBase> controllerToHandChecksStage2 = new List<InputCheckBase>()
            {
                new DistanceBetweenInputs()
                {
                    inputMethodType= InputMethodType.XRController,
                    actionThreshold = 0.2f
                },
                new PinchGrasp()
                {
                    inputMethodType = InputMethodType.LeapHand
                },
                new InputVelocity()
                {
                    inputMethodType = InputMethodType.XRController,
                    velocityIsLower = true,
                    actionThreshold = 0.01f,
                    useTime = true,
                    timeThreshold = 0.1f
                }
            };

            leftHandChecks = new List<InputCheckStage>()
            {
                new InputCheckStage(handToControllerChecksStage0),
                new InputCheckStage(handToControllerChecksStage1),
            };
            leftHandChecks.ForEach(inputCheckStage => inputCheckStage.SetInputCheckChirality(Chirality.Left));

            leftControllerChecks = new List<InputCheckStage>()
            {
                new InputCheckStage(controllerToHandChecksStage0),
                new InputCheckStage(controllerToHandChecksStage1),
                new InputCheckStage(controllerToHandChecksStage2),
            };
            leftControllerChecks.ForEach(inputCheckStage => inputCheckStage.SetInputCheckChirality(Chirality.Left));

            rightHandChecks = new List<InputCheckStage>(leftHandChecks);
            rightHandChecks.ForEach(inputCheckStage => inputCheckStage.SetInputCheckChirality(Chirality.Right));

            rightControllerChecks = new List<InputCheckStage>(leftControllerChecks);
            rightControllerChecks.ForEach(inputCheckStage => inputCheckStage.SetInputCheckChirality(Chirality.Right));
        }
    }
}
