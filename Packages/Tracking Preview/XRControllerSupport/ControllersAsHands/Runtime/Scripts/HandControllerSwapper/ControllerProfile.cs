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
    public class ControllerProfile 
    {
        public List<HandChecks> leftHandChecks = new List<HandChecks>();
        public List<HandChecks> rightHandChecks = new List<HandChecks>();

        public List<HandChecks> leftControllerChecks = new List<HandChecks>();
        public List<HandChecks> rightControllerChecks = new List<HandChecks>();

        public ControllerProfile()
        {
            PopulateLeftHandChecks();
            PopulateRightHandChecks();
        }

        public class HandChecks
        {
            public List<InputCheckBase> checks = new List<InputCheckBase>();
            
            public HandChecks(List<InputCheckBase> checks)
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

        private void SetupProfiles(List<HandChecks> checks, LeapProvider provider)
        {
            for (int i = 0; i < checks.Count; i++)
            {
                for (int j = 0; j < checks[i].checks.Count; j++)
                {
                    checks[i].checks[j].Setup(provider);
                }
            }
        }

        private void PopulateLeftHandChecks()
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

            leftHandChecks = new List<HandChecks>()
            {
                new HandChecks(handToControllerChecksStage0),
                new HandChecks(handToControllerChecksStage1),
            };
            leftHandChecks.ForEach(handCheck => handCheck.SetInputCheckChirality(Chirality.Left));

            leftControllerChecks = new List<HandChecks>()
            {
                new HandChecks(controllerToHandChecksStage0),
                new HandChecks(controllerToHandChecksStage1),
                new HandChecks(controllerToHandChecksStage2),
            };
            leftControllerChecks.ForEach(handCheck => handCheck.SetInputCheckChirality(Chirality.Left));
        }
        
        private void PopulateRightHandChecks()
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

            rightHandChecks = new List<HandChecks>()
            {
                new HandChecks(handToControllerChecksStage0),
                new HandChecks(handToControllerChecksStage1),
            };
            rightHandChecks.ForEach(handCheck => handCheck.SetInputCheckChirality(Chirality.Right));


            rightControllerChecks = new List<HandChecks>()
            {
                new HandChecks(controllerToHandChecksStage0),
                new HandChecks(controllerToHandChecksStage1),
                new HandChecks(controllerToHandChecksStage2),
            };
            rightControllerChecks.ForEach(handCheck => handCheck.SetInputCheckChirality(Chirality.Right));
        }
    }
}
