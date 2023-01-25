/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;

namespace Leap.Unity.Controllers
{

    /// <summary>
    /// ControllerProfile sets up and stores the various InputChecks and is primarily used
    /// as a data object
    /// </summary>
    public class ControllerProfile
    {
        public readonly List<InputCheckStage> leftHandChecks = new List<InputCheckStage>();
        public readonly List<InputCheckStage> rightHandChecks = new List<InputCheckStage>();

        public readonly List<InputCheckStage> leftControllerChecks = new List<InputCheckStage>();
        public readonly List<InputCheckStage> rightControllerChecks = new List<InputCheckStage>();

        public ControllerProfile()
        {
            PopulateHandToControllerInputCheckStagesList(out leftHandChecks, Chirality.Left);
            PopulateHandToControllerInputCheckStagesList(out rightHandChecks, Chirality.Right);

            PopulateControllerToHandInputCheckStagesList(out leftControllerChecks, Chirality.Left);
            PopulateControllerToHandInputCheckStagesList(out rightControllerChecks, Chirality.Right);
        }

        public class InputCheckStage
        {
            public List<InputCheckBase> Checks;

            public InputCheckStage()
            {
                Checks = new List<InputCheckBase>();
            }

            public InputCheckStage(List<InputCheckBase> checks)
            {
                this.Checks = checks;
            }

            public void SetInputCheckChirality(Chirality chirality)
            {
                Checks.ForEach(check => check.hand = chirality);
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
                for (int j = 0; j < checks[i].Checks.Count; j++)
                {
                    checks[i].Checks[j].Setup(provider);
                }
            }
        }

        private void PopulateHandToControllerInputCheckStagesList(out List<InputCheckStage> inputCheckStages, Chirality chirality)
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

            inputCheckStages = new List<InputCheckStage>()
            {
                new InputCheckStage(handToControllerChecksStage0),
                new InputCheckStage(handToControllerChecksStage1),
            };
            inputCheckStages.ForEach(inputCheckStage => inputCheckStage.SetInputCheckChirality(chirality));
        }

        private void PopulateControllerToHandInputCheckStagesList(out List<InputCheckStage> inputCheckStages, Chirality chirality)
        {
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