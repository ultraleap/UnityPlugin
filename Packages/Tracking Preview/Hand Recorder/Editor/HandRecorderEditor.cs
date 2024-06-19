using UnityEditor;
using UnityEngine;

namespace Ultraleap.Recording
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

            string buttonLabel = target.recording ? "STOP RECORDING" : "START RECORDING";
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
                HandRecorder recorder = duplicate.GetComponent<HandRecorder>();
                DestroyImmediate(recorder);

                HandEnableDisable[] handEnableDisables = duplicate.GetComponentsInChildren<HandEnableDisable>(true);

                foreach (HandEnableDisable handEnableDisable in handEnableDisables)
                {
                    DestroyImmediate(handEnableDisable);
                }

                HandModelBase[] handModelBases = duplicate.GetComponentsInChildren<HandModelBase>(true);

                foreach (HandModelBase handModelBase in handModelBases)
                {
                    DestroyImmediate(handModelBase);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}