/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(InteractionXRController), true)]
    public class InteractionVRControllerEditor : InteractionControllerEditor
    {

        private List<InteractionXRController> _vrControllers;

        bool _pluralPossibleControllers = false;

        protected override void OnEnable()
        {
            base.OnEnable();

            //_vrController = (target as InteractionVRController);
            _vrControllers = targets
                                    .Where(c => c is InteractionXRController)
                                    .Cast<InteractionXRController>()
                                    .ToList();
            _pluralPossibleControllers = _vrControllers.Count > 1;

            specifyCustomPostDecorator("graspButtonAxis", drawGraspButtonAxisDecorator);
        }

        private void drawGraspButtonAxisDecorator(SerializedProperty property)
        {
            // Whether the axis is overriden.
            int numGraspAxisOverrides = _vrControllers
                                                      .Where(c => c.graspAxisOverride != null)
                                                      .Count();
            bool anyGraspAxisOverrides = numGraspAxisOverrides > 0;

            if (anyGraspAxisOverrides)
            {
                string graspAxisOverrideMessage;
                if (_pluralPossibleControllers)
                {
                    graspAxisOverrideMessage = "One or more currently selected interaction VR "
                                             + "controllers has their grasping axis overridden, "
                                             + "so their graspButtonAxis settings will be ignored.";
                }
                else
                {
                    graspAxisOverrideMessage = "This interaction VR controller has its grasping "
                                             + "axis overridden, so the graspButtonAxis setting "
                                             + "will be ignored.";
                }
                EditorGUILayout.HelpBox(graspAxisOverrideMessage, MessageType.Info);
            }

            // Whether the axis is valid.
            bool anyInvalidGraspAxes = _vrControllers
                                                     .Select(c => isGraspAxisConfigured(c))
                                                     .Where(b => b == false)
                                                     .Any();

            if (anyInvalidGraspAxes)
            {
                string graspAxisInvalidMessage;
                if (_pluralPossibleControllers)
                {
                    graspAxisInvalidMessage = "One or more currently selected interaction VR "
                                            + "controllers is configured with a grasping axis name "
                                            + "that is not set up in Unity's Input settings.";
                }
                else
                {
                    graspAxisInvalidMessage = "This interaction VR controller is configured with a "
                                            + "grasping axis name that is not set up in Unity's "
                                            + "Input settings.";
                }
                graspAxisInvalidMessage += " Check your input settings via Edit -> Project "
                                          + "Settings -> Input. Otherwise, this interaction "
                                          + "controller will be unable to grasp objects.";

                EditorGUILayout.HelpBox(graspAxisInvalidMessage, MessageType.Warning);
            }
        }

        private bool isGraspAxisConfigured(InteractionXRController controller)
        {
            try
            {
                Input.GetAxis(controller.graspButtonAxis);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

    }

}