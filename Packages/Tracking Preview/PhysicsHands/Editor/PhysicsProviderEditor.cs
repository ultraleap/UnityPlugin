using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    [CustomEditor(typeof(PhysicsProvider))]
    public class PhysicsProviderEditor : Editor
    {
        PhysicsProvider _physicsProvider;

        SerializedProperty _inputProvider;

        GUIContent _physicsLayersText;

        SerializedProperty _defaultLayer;
        SerializedProperty _interactableLayers;
        SerializedProperty _noContactLayers;

        SerializedProperty _automaticHandsLayer, _automaticHandsResetLayer;
        SerializedProperty _handsLayer, _handsResetLayer;

        SerializedProperty _interHandCollisions;
        SerializedProperty _strength, _perBoneMass;
        SerializedProperty _handTeleportDistance, _handGraspTeleportDistance;

        SerializedProperty _enableHelpers;
        SerializedProperty _helperMovesObjects;
        SerializedProperty _interpolateMass, _maxMass;


        private void Awake()
        {
            _physicsLayersText = new GUIContent("Physics Layers Setup", "Due to the complexity of the physics layer matrix, the system will automatically apply collisions between assigned layers.");
        }

        private void GetProperties()
        {
            _physicsProvider = (PhysicsProvider)serializedObject.targetObject;
            _inputProvider = serializedObject.FindProperty("_inputLeapProvider");

            _defaultLayer = serializedObject.FindProperty("_defaultLayer");
            _interactableLayers = serializedObject.FindProperty("_interactableLayers");
            _noContactLayers = serializedObject.FindProperty("_noContactLayers");

            _automaticHandsLayer = serializedObject.FindProperty("_automaticHandsLayer");
            _automaticHandsResetLayer = serializedObject.FindProperty("_automaticHandsResetLayer");
            _handsLayer = serializedObject.FindProperty("_handsLayer");
            _handsResetLayer = serializedObject.FindProperty("_handsResetLayer");

            _interHandCollisions = serializedObject.FindProperty("_interHandCollisions");
            _strength = serializedObject.FindProperty("_strength");
            _perBoneMass = serializedObject.FindProperty("_perBoneMass");
            _handTeleportDistance = serializedObject.FindProperty("_handTeleportDistance");
            _handGraspTeleportDistance = serializedObject.FindProperty("_handGraspTeleportDistance");

            _enableHelpers = serializedObject.FindProperty("_enableHelpers");
            _helperMovesObjects = serializedObject.FindProperty("_helperMovesObjects");
            _interpolateMass = serializedObject.FindProperty("_interpolateMass");
            _maxMass = serializedObject.FindProperty("_maxMass");

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"), true, new GUILayoutOption[0]);
            GUI.enabled = true;
        }

        public override void OnInspectorGUI()
        {
            GetProperties();

            SetupSection();

            PhysicsSection();

            HandsSection();

            HelperSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void SetupSection()
        {
            EditorGUILayout.LabelField("Provider Setup", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_physicsProvider.LeftHand == null && _physicsProvider.RightHand == null)
            {
                _physicsProvider.GenerateHands();
                serializedObject.Update();
            }
            else
            {
                if (GUILayout.Button("Re-generate Hands"))
                {
                    if (_physicsProvider.LeftHand != null)
                    {
                        DestroyImmediate(_physicsProvider.LeftHand.gameObject);
                    }
                    if (_physicsProvider.RightHand != null)
                    {
                        DestroyImmediate(_physicsProvider.RightHand.gameObject);
                    }
                    _physicsProvider.GenerateHands();

                    serializedObject.Update();
                }
            }

            if (_physicsProvider.LeftHand != null)
            {
                UncheckHand(_physicsProvider.LeftHand);
            }

            if (_physicsProvider.RightHand != null)
            {
                UncheckHand(_physicsProvider.RightHand);
            }

            EditorGUILayout.PropertyField(_inputProvider);
            EditorGUILayout.EndVertical();
        }

        private void UncheckHand(PhysicsHand hand)
        {
            if (hand != null && hand.GetPhysicsHand() != null && hand.Bodies != null)
            {
                for (int i = 0; i < hand.Bodies.Length; i++)
                {
                    if (hand.Bodies[i] == null)
                        continue;

                    SerializedObject so = new SerializedObject(hand.Bodies[i]);

#if UNITY_2020_3
                    so.FindProperty("m_ComputeParentAnchor").boolValue = false;
#endif
#if UNITY_2021_2_OR_NEWER
                    so.FindProperty("m_MatchAnchors").boolValue = false; // this should still work
#endif
                    so.ApplyModifiedProperties();
                }
            }
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField($"{hand.Handedness.ToString()} Hand", hand, typeof(PhysicsHand), true);
            EditorGUI.EndDisabledGroup();
        }

        private void PhysicsSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_physicsLayersText, EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Object Layers", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_defaultLayer);
            EditorGUILayout.PropertyField(_interactableLayers);
            EditorGUILayout.PropertyField(_noContactLayers);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Hand Layers", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_automaticHandsLayer);
            if (!_automaticHandsLayer.boolValue)
            {
                EditorGUILayout.PropertyField(_handsLayer);
            }
            EditorGUILayout.PropertyField(_automaticHandsResetLayer);
            if (!_automaticHandsResetLayer.boolValue)
            {
                EditorGUILayout.PropertyField(_handsResetLayer);
            }
            EditorGUILayout.EndVertical();
        }

        private void HandsSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Physics Hands Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(_interHandCollisions);
            EditorGUILayout.PropertyField(_strength);
            EditorGUILayout.PropertyField(_perBoneMass);
            EditorGUILayout.PropertyField(_handTeleportDistance);
            EditorGUILayout.PropertyField(_handGraspTeleportDistance);
            EditorGUILayout.EndVertical();
        }

        private void HelperSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Helpers", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(_enableHelpers);
            if (_enableHelpers.boolValue)
            {
                EditorGUILayout.PropertyField(_helperMovesObjects);
                EditorGUILayout.PropertyField(_interpolateMass);
                if (_interpolateMass.boolValue)
                {
                    EditorGUILayout.PropertyField(_maxMass);
                }
            }
            EditorGUILayout.EndVertical();
        }

    }
}