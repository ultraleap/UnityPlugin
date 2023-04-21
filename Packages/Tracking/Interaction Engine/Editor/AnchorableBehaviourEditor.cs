/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(AnchorableBehaviour))]
    public class AnchorableBehaviourEditor : CustomEditorBase<AnchorableBehaviour>
    {

        protected override void OnEnable()
        {
            base.OnEnable();

            deferProperty("_eventTable");
            specifyCustomDrawer("_eventTable", drawEventTable);

            specifyConditionalDrawing("lockWhenAttached",
                                      "matchAnchorMotionWhileAttaching");

            specifyConditionalDrawing("useTrajectory",
                                      "_motionlessRangeFraction",
                                      "_maxMotionlessRange",
                                      "_maxAttachmentAngle");

            specifyConditionalDrawing(() => { return target.interactionBehaviour != null; },
                                      "detachWhenGrasped",
                                      "_tryAnchorNearestOnGraspEnd",
                                      "isAttractedByHand",
                                      "maxAttractionReach",
                                      "attractionReachByDistance");

            specifyConditionalDrawing("isAttractedByHand",
                                      "maxAttractionReach",
                                      "attractionReachByDistance",
                                      "anchorHandAttractionRate");
        }

        private EnumEventTableEditor _tableEditor;
        private void drawEventTable(SerializedProperty property)
        {
            if (_tableEditor == null)
            {
                _tableEditor = new EnumEventTableEditor(property, typeof(AnchorableBehaviour.EventType));
            }

            _tableEditor.DoGuiLayout();
        }

        public override void OnInspectorGUI()
        {
            drawWarningMessages();

            drawAttachmentHelperButtons();

            base.OnInspectorGUI();
        }

        private void drawWarningMessages()
        {
            // While the editor application is playing, we expect there to be at least the empty lambda that initializes the Action
            // and the UnityEvent subscription.
            // While in edit-mode, we only expect there to be the empty lambda that initializes the Action.
            int expectedMinimumActionListeners = EditorApplication.isPlaying ? 2 : 1;

            bool hasInvalidPostGraspEndCallback = !target.tryAnchorNearestOnGraspEnd
                                               && (target.OnPostTryAnchorOnGraspEnd.GetInvocationList().Length > expectedMinimumActionListeners
                                                   || (_tableEditor != null &&
                                                       _tableEditor.HasAnyCallbacks((int)AnchorableBehaviour.EventType.OnPostTryAnchorOnGraspEnd)));
            if (hasInvalidPostGraspEndCallback)
            {
                EditorGUILayout.HelpBox("This object's OnPostObjectGraspEnd is subscribed to, but the event will never "
                                      + "fire because tryAnchorNearestOnGraspEnd is disabled.",
                                        MessageType.Warning);
            }
        }

        private void drawAttachmentHelperButtons()
        {
            if (!EditorApplication.isPlaying)
            {
                // Attach / Detach Object
                EditorGUILayout.BeginHorizontal();

                var anyTargetsCanAnchor = targets.Any(t => t.anchor != null && !target.isAttached);

                EditorGUI.BeginDisabledGroup(!anyTargetsCanAnchor);
                if (GUILayout.Button(new GUIContent("Attach Object" + (targets.Length > 1 ? "s" : ""),
                                                    "Will attach the object to its anchor. If the object is not currently at its anchor, "
                                                  + "currently at its anchor, it will begin move to it when play mode begins.")))
                {
                    Undo.IncrementCurrentGroup();
                    foreach (var singleTarget in targets)
                    {
                        Undo.RecordObject(singleTarget, "Try Attach Object");
                        singleTarget.TryAttach(ignoreRange: true);
                    }
                }
                EditorGUI.EndDisabledGroup();

                var anyTargetsCanDetach = targets.Any(t => t.isAttached);

                EditorGUI.BeginDisabledGroup(!anyTargetsCanDetach);
                if (GUILayout.Button(new GUIContent("Detach Object" + (targets.Length > 1 ? "s" : ""),
                                                    "Will detach the object from its anchor. AnchorableBehaviours won't seek out an anchor "
                                                  + "until they are specifically told to attach to one.")))
                {
                    Undo.IncrementCurrentGroup();
                    foreach (var singleTarget in targets)
                    {
                        Undo.RecordObject(singleTarget, "Try Detach Object");
                        singleTarget.Detach();
                    }
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                // Move Object to Anchor

                bool anyTranslatedFromAnchor = false;
                bool anyRotatedFromAnchor = false;

                foreach (var singleTarget in targets)
                {
                    anyTranslatedFromAnchor |= singleTarget.anchor != null && Vector3.Distance(singleTarget.transform.position, singleTarget.anchor.transform.position) > 0.0001F;
                    anyRotatedFromAnchor |= singleTarget.anchor != null && singleTarget.anchorRotation
                                                                            && Quaternion.Angle(singleTarget.transform.rotation, singleTarget.anchor.transform.rotation) > 0.1F;
                }

                if (anyTranslatedFromAnchor || anyRotatedFromAnchor)
                {
                    if (GUILayout.Button(new GUIContent("Move Object" + (targets.Length > 1 ? "s" : "") + " To Anchor",
                                                        "Detected that the object is not currently at its anchor, but upon pressing play, "
                                                      + "the object will move to to match its anchor. If you'd like the object to move to "
                                                      + "its anchor now, click this button.")))
                    {
                        Undo.IncrementCurrentGroup();
                        foreach (var singleTarget in targets)
                        {
                            Undo.RecordObject(singleTarget.transform, "Move Target Transform to Anchor");
                            singleTarget.transform.position = singleTarget.anchor.transform.position;
                            if (singleTarget.anchorRotation) singleTarget.transform.rotation = singleTarget.anchor.transform.rotation;
                        }
                    }
                }
            }
        }
    }
}