/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Linq;
using UnityEditor;

namespace Leap.Unity.Interaction
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(InteractionController), true)]
    public class InteractionControllerEditor : CustomEditorBase<InteractionController>
    {
        public override void OnInspectorGUI()
        {
            checkParentedToManager();
            checkPrimaryHoverPoints();

            base.OnInspectorGUI();
        }

        private void checkPrimaryHoverPoints()
        {
            bool anyPrimaryHoverPoints = false;
            bool anyWithNoPrimaryHoverPoints = false;
            foreach (var singleTarget in targets)
            {
                anyPrimaryHoverPoints = false;
                foreach (var primaryHoverPoint in singleTarget.primaryHoverPoints)
                {
                    if (primaryHoverPoint != null)
                    {
                        anyPrimaryHoverPoints = true;
                        break;
                    }
                }

                if (singleTarget.intHand != null)
                {
                    for (int i = 0; i < singleTarget.intHand.enabledPrimaryHoverFingertips.Length; i++)
                    {
                        if (singleTarget.intHand.enabledPrimaryHoverFingertips[i])
                        {
                            anyPrimaryHoverPoints = true;
                            break;
                        }
                    }
                }

                if (!anyPrimaryHoverPoints)
                {
                    anyWithNoPrimaryHoverPoints = true;
                    break;
                }
            }

            if (anyWithNoPrimaryHoverPoints)
            {
                string message = "No primary hover points found for this interaction controller. "
                               + "This controller will never trigger primary hover for an object. "
                               + "UI elements such as InteractionButton and InteractionSlider "
                               + "will not be able to interact with this interaction controller.";
                EditorGUILayout.HelpBox(message, MessageType.Warning);
            }
        }

        private void checkParentedToManager()
        {
            bool plural = targets.Length > 1;
            bool anyNotParentedToInteractionManager;

            anyNotParentedToInteractionManager = targets.Any(c => c.GetComponentInParent<InteractionManager>() == null);

            if (anyNotParentedToInteractionManager)
            {
                string message = "";
                if (plural)
                {
                    message += "One of more currently selected controllers ";
                }
                else
                {
                    message += "The currently selected controller ";
                }

                message += "is not the child of an Interaction Manager. Interaction Controllers "
                         + "must be childed to an Interaction Manager in order to function.";

                EditorGUILayout.HelpBox(message, MessageType.Warning);
            }
        }
    }
}