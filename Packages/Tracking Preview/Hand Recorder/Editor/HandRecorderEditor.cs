using UnityEditor;
using UnityEngine;

namespace Leap.Recording
{
    [CustomEditor(typeof(HandRecorder))]
    [CanEditMultipleObjects]
    public class HandRecorderEditor : CustomEditorBase<HandRecorder>
    {
        private enum HandBaseType
        {
            WRIST,
            ELBOW,
            NONSTANDARD
        };

        protected override void OnEnable()
        {
            base.OnEnable();

            specifyConditionalDrawing("handsToRecord", new int[]{
                                            (int)HandRecorder.ChiralityOptions.BOTH,
                                            (int)HandRecorder.ChiralityOptions.LEFT,
                                            (int)HandRecorder.ChiralityOptions.RIGHT}, "handSelection");

            specifyConditionalDrawing("handSelection", (int)HandRecorder.HandSelection.MANUAL, "leftHandBoneRoot", "rightHandBoneRoot");

            specifyConditionalDrawing("handsToRecord", new int[]{
                                            (int)HandRecorder.ChiralityOptions.BOTH,
                                            (int)HandRecorder.ChiralityOptions.LEFT}, "leftHandBoneRoot");

            specifyConditionalDrawing("handsToRecord", new int[]{
                                            (int)HandRecorder.ChiralityOptions.BOTH,
                                            (int)HandRecorder.ChiralityOptions.RIGHT}, "rightHandBoneRoot");

            specifyConditionalDrawing(() => { return !target.automaticallyGenerateAnimationClip; }, "targetClip");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);

            var buttonLabel = target.recording ? "STOP RECORDING" : "START RECORDING";
            if (GUILayout.Button(buttonLabel))
            {
                if (target.recording)
                {
                    target.EndRecording();
                }
                else
                {
                    target.StartRecording();
                }
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Generate Playback Copy"))
            {
                GameObject duplicate = Instantiate(target.gameObject);
                duplicate.name = target.name + " Playback Copy";

                // Add new components
                duplicate.AddComponent<Animator>();

                // Remove unnecessary component for a playback copy
                var recorder = duplicate.GetComponent<HandRecorder>();
                DestroyImmediate(recorder);

                var handEnableDisables = duplicate.GetComponentsInChildren<HandEnableDisable>(true);

                foreach (var handEnableDisable in handEnableDisables)
                {
                    DestroyImmediate(handEnableDisable);
                }

                var handModelBases = duplicate.GetComponentsInChildren<HandModelBase>(true);

                foreach (var handModelBase in handModelBases)
                {
                    DestroyImmediate(handModelBase);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}