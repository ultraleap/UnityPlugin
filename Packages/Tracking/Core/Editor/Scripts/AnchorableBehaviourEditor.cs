/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(AnchorableBehaviour))]
    public class AnchorableBehaviourEditor : CustomEditorBase<AnchorableBehaviour>
    {

        protected override void OnEnable()
        {
            base.OnEnable();

            deferProperty("_eventTable");
            specifyCustomDrawer("_eventTable", DrawEventTable);

            specifyConditionalDrawing("lockWhenAttached",
                                      "matchAnchorMotionWhileAttaching");

            specifyConditionalDrawing("useTrajectory",
                                      "_motionlessRangeFraction",
                                      "_maxMotionlessRange",
                                      "_maxAttachmentAngle");

            specifyConditionalDrawing("isAttractedByHand",
                                      "maxAttractionReach",
                                      "attractionReachByDistance",
                                      "anchorHandAttractionRate");
        }

        private EnumEventTableEditor tableEditor;
        private void DrawEventTable(SerializedProperty property)
        {
            if (tableEditor == null)
            {
                tableEditor = new EnumEventTableEditor(property, typeof(AnchorableBehaviour.EventType));
            }

            tableEditor.DoGuiLayout();
        }

        public override void OnInspectorGUI()
        {
            DrawWarningMessages();

            DrawAttachmentHelperButtons();

            base.OnInspectorGUI();
        }

        private void DrawWarningMessages()
        {
            // While the editor application is playing, we expect there to be at least the empty lambda that initializes the Action
            // and the UnityEvent subscription.
            // While in edit-mode, we only expect there to be the empty lambda that initializes the Action.
            int _expectedMinimumActionListeners = EditorApplication.isPlaying ? 2 : 1;

            bool _hasInvalidPostGraspEndCallback = !target.TryAnchorNearestOnGraspEnd
                                               && (target.OnPostTryAnchorOnGraspEnd.GetInvocationList().Length > _expectedMinimumActionListeners
                                                   || (tableEditor != null &&
                                                       tableEditor.HasAnyCallbacks((int)AnchorableBehaviour.EventType.OnPostTryAnchorOnGraspEnd)));
            if (_hasInvalidPostGraspEndCallback)
            {
                EditorGUILayout.HelpBox("This object's OnPostObjectGraspEnd is subscribed to, but the event will never "
                                      + "fire because tryAnchorNearestOnGraspEnd is disabled.",
                                        MessageType.Warning);
            }
        }

        private void DrawAttachmentHelperButtons()
        {
            if (!EditorApplication.isPlaying)
            {
                // Attach / Detach Object
                EditorGUILayout.BeginHorizontal();

                var _anyTargetsCanAnchor = targets.Any(t => t.Anchor != null && !target.isAttached);

                EditorGUI.BeginDisabledGroup(!_anyTargetsCanAnchor);
                if (GUILayout.Button(new GUIContent("Attach Object" + (targets.Length > 1 ? "s" : ""),
                                                    "Will attach the object to its anchor. If the object is not currently at its anchor, "
                                                  + "currently at its anchor, it will begin move to it when play mode begins.")))
                {
                    Undo.IncrementCurrentGroup();
                    foreach (var _singleTarget in targets)
                    {
                        Undo.RecordObject(_singleTarget, "Try Attach Object");
                        _singleTarget.TryAttach(ignoreRange: true);
                    }
                }
                EditorGUI.EndDisabledGroup();

                var _anyTargetsCanDetach = targets.Any(t => t.isAttached);

                EditorGUI.BeginDisabledGroup(!_anyTargetsCanDetach);
                if (GUILayout.Button(new GUIContent("Detach Object" + (targets.Length > 1 ? "s" : ""),
                                                    "Will detach the object from its anchor. AnchorableBehaviours won't seek out an anchor "
                                                  + "until they are specifically told to attach to one.")))
                {
                    Undo.IncrementCurrentGroup();
                    foreach (var _singleTarget in targets)
                    {
                        Undo.RecordObject(_singleTarget, "Try Detach Object");
                        _singleTarget.Detach();
                    }
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                // Move Object to Anchor

                bool _anyTranslatedFromAnchor = false;
                bool _anyRotatedFromAnchor = false;

                foreach (var _singleTarget in targets)
                {
                    _anyTranslatedFromAnchor |= _singleTarget.Anchor != null && Vector3.Distance(_singleTarget.transform.position, _singleTarget.Anchor.transform.position) > 0.0001F;
                    _anyRotatedFromAnchor |= _singleTarget.Anchor != null && _singleTarget.anchorRotation
                                                                            && Quaternion.Angle(_singleTarget.transform.rotation, _singleTarget.Anchor.transform.rotation) > 0.1F;
                }

                if (_anyTranslatedFromAnchor || _anyRotatedFromAnchor)
                {
                    if (GUILayout.Button(new GUIContent("Move Object" + (targets.Length > 1 ? "s" : "") + " To Anchor",
                                                        "Detected that the object is not currently at its anchor, but upon pressing play, "
                                                      + "the object will move to to match its anchor. If you'd like the object to move to "
                                                      + "its anchor now, click this button.")))
                    {
                        Undo.IncrementCurrentGroup();
                        foreach (var _singleTarget in targets)
                        {
                            Undo.RecordObject(_singleTarget.transform, "Move Target Transform to Anchor");
                            _singleTarget.transform.position = _singleTarget.Anchor.transform.position;
                            if (_singleTarget.anchorRotation) _singleTarget.transform.rotation = _singleTarget.Anchor.transform.rotation;
                        }
                    }
                }
            }
        }
    }
}