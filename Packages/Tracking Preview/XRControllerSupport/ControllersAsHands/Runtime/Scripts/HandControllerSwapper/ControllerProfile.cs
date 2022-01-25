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
using System.Linq;

namespace Leap.Unity.Controllers
{

    /// <summary>
    /// ControllerProfile sets up and stores the various InputChecks and is primarily used
    /// as a data object
    /// </summary>
    public class ControllerProfile
    {
        public List<InputCheckStage> LeftHandChecks = new List<InputCheckStage>();
        public List<InputCheckStage> RightHandChecks = new List<InputCheckStage>();

        public List<InputCheckStage> LeftControllerChecks = new List<InputCheckStage>();
        public List<InputCheckStage> RightControllerChecks = new List<InputCheckStage>();

        public ControllerProfile()
        {
            PopulateHandToControllerInputCheckStagesList(ref LeftHandChecks, Chirality.Left);
            PopulateHandToControllerInputCheckStagesList(ref RightHandChecks, Chirality.Right);

            PopulateControllerToHandInputCheckStagesList(ref LeftControllerChecks, Chirality.Left);
            PopulateControllerToHandInputCheckStagesList(ref RightControllerChecks, Chirality.Right);
        }

        public class InputCheckStage
        {
            public List<InputCheckBase> checks = new List<InputCheckBase>();

            public InputCheckStage()
            {
                checks = new List<InputCheckBase>();
            }

            public InputCheckStage(List<InputCheckBase> checks)
            {
                this.checks = checks;
            }

            public void SetInputCheckChirality(Chirality chirality)
            {
                checks.ForEach(check => check.Hand = chirality);
            }

        }

        public void SetupProfiles(LeapProvider originalProvider)
        {
            SetupProfiles(LeftHandChecks, originalProvider);
            SetupProfiles(RightHandChecks, originalProvider);
            SetupProfiles(LeftControllerChecks, originalProvider);
            SetupProfiles(RightControllerChecks, originalProvider);
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

        private void PopulateHandToControllerInputCheckStagesList(ref List<InputCheckStage> inputCheckStages, Chirality chirality)
        {
            List<InputCheckBase> handToControllerChecksStage0 = new List<InputCheckBase>()
            {
                new DistanceBetweenInputs()
                {
                    InputMethodType = InputMethodType.LeapHand,
                    ActionThreshold = 0.12f
                },
                new HasButtonBeenPressed()
                {
                    InputMethodType = InputMethodType.XRController,
                },
                new InputIsInactive()
                {
                    InputMethodType = InputMethodType.LeapHand,
                },
            };

            List<InputCheckBase> handToControllerChecksStage1 = new List<InputCheckBase>()
            {
               new DistanceBetweenInputs()
               {
                    InputMethodType = InputMethodType.LeapHand,
                    ActionThreshold = 0.12f,
                    UseTime = true,
                    TimeThreshold = 2,
               },
               new HasButtonBeenPressed()
               {
                    InputMethodType = InputMethodType.XRController,
               },
               new InputVelocity()
               {
                    InputMethodType = InputMethodType.XRController,
                    velocityIsLower = false,
                    ActionThreshold = 0.015f,
                    UseTime = true,
                    TimeThreshold = 0.2f
               }
            };

            inputCheckStages = new List<InputCheckStage>()
            {
                new InputCheckStage(handToControllerChecksStage0),
                new InputCheckStage(handToControllerChecksStage1),
            };
            inputCheckStages.ForEach(inputCheckStage => inputCheckStage.SetInputCheckChirality(chirality));
        }

        private void PopulateControllerToHandInputCheckStagesList(ref List<InputCheckStage> inputCheckStages, Chirality chirality)
        {
            List<InputCheckBase> controllerToHandChecksStage0 = new List<InputCheckBase>()
            {
                new DistanceFromHead()
                {
                    InputMethodType = InputMethodType.XRController,
                    LessThan = false,
                    ActionThreshold = 1.1f,
                    UseTime = true,
                    TimeThreshold = 0.1f
                },
                new InputVelocity()
                {
                    InputMethodType = InputMethodType.XRController,
                    velocityIsLower = true,
                    ActionThreshold = 0.01f,
                    UseTime = true,
                    TimeThreshold = 0.2f,
                },
                new IsFacingDown()
                {
                    InputMethodType = InputMethodType.XRController,
                    UseTime = true,
                    ActionThreshold = 30,
                    TimeThreshold = 0.2f
                }
            };

            List<InputCheckBase> controllerToHandChecksStage1 = new List<InputCheckBase>()
            {
                new DistanceBetweenInputs()
                {
                    InputMethodType = InputMethodType.XRController,
                    ActionThreshold = 0.2f
                },
            };

            List<InputCheckBase> controllerToHandChecksStage2 = new List<InputCheckBase>()
            {
                new DistanceBetweenInputs()
                {
                    InputMethodType= InputMethodType.XRController,
                    ActionThreshold = 0.2f
                },
                new PinchGrasp()
                {
                    InputMethodType = InputMethodType.LeapHand
                },
                new InputVelocity()
                {
                    InputMethodType = InputMethodType.XRController,
                    velocityIsLower = true,
                    ActionThreshold = 0.01f,
                    UseTime = true,
                    TimeThreshold = 0.1f
                }
            };

            inputCheckStages = new List<InputCheckStage>()
            {
                new InputCheckStage(controllerToHandChecksStage0),
                new InputCheckStage(controllerToHandChecksStage1),
                new InputCheckStage(controllerToHandChecksStage2),
            };

            inputCheckStages.ForEach(inputCheckStage => inputCheckStage.SetInputCheckChirality(chirality));
        }
    }
}
