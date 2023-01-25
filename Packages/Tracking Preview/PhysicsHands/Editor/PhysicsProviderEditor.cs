using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    [CustomEditor(typeof(PhysicsProvider))]
    public class PhysicsProviderEditor : Editor
    {
        private const int RECOMMENDED_SOLVER_ITERATIONS = 15;
        private const int RECOMMENDED_SOLVER_VELOCITY_ITERATIONS = 5;
        private const float RECOMMENDED_TIMESTEP = 0.011111f;

        private readonly int[] HAND_SOLVER_ITERATIONS = { 10, 15, 30 };
        private readonly int[] HAND_SOLVER_VELOCITY_ITERATIONS = { 4, 5, 10 };
        private readonly string[] HAND_SOLVER_NAMES = { "Low", "Medium", "High", "Custom" };
        private int _currentPreset = -1;

        PhysicsProvider _physicsProvider;

        SerializedProperty _inputProvider;

        GUIContent _physicsLayersText, _solverPresetText, _projectSolverText, _projectSolverVelocityText;

        SerializedProperty _defaultLayer;
        SerializedProperty _interactableLayers;
        SerializedProperty _noContactLayers;

        SerializedProperty _automaticHandsLayer, _automaticHandsResetLayer;
        SerializedProperty _handsLayer, _handsResetLayer;

        SerializedProperty _interHandCollisions;
        SerializedProperty _strength, _perBoneMass;
        SerializedProperty _handTeleportDistance, _handGraspTeleportDistance;
        SerializedProperty _handSolverIterations, _handSolverVelocityIterations;

        SerializedProperty _enableHelpers;
        SerializedProperty _helperMovesObjects;
        SerializedProperty _interpolateMass, _maxMass;
        SerializedProperty _enhanceThrowing;


        private void Awake()
        {
            _physicsLayersText = new GUIContent("Layer Setup", "Due to the complexity of the physics layer matrix, the system will automatically apply collisions between assigned layers.");
            _solverPresetText = new GUIContent("Solver Quality Preset", "These presets will adjust both how robust the physics calculations are for your hands, and their overall performance cost. " +
                "This will only apply to the hands objects and can be different to your project settings. You can modify these settings during play mode to rapidly test different setups.");

            _projectSolverText = new GUIContent("Project Solver Iterations", "The solver iterations of the physics simulation used throughout your project. This can be different to your hands.");
            _projectSolverVelocityText = new GUIContent("Project Solver Velocity Iterations", "The solver iterations of the physics simulation for calculating velocity used throughout your project. This can be different to your hands.");
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
            _handSolverIterations = serializedObject.FindProperty("_handSolverIterations");
            _handSolverVelocityIterations = serializedObject.FindProperty("_handSolverVelocityIterations");

            _enableHelpers = serializedObject.FindProperty("_enableHelpers");
            _helperMovesObjects = serializedObject.FindProperty("_helperMovesObjects");
            _interpolateMass = serializedObject.FindProperty("_interpolateMass");
            _maxMass = serializedObject.FindProperty("_maxMass");
            _enhanceThrowing = serializedObject.FindProperty("_enhanceThrowing");

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"), true, new GUILayoutOption[0]);
            GUI.enabled = true;
        }

        public override void OnInspectorGUI()
        {
            GetProperties();

            WarningsSection();

            SetupSection();

            PhysicsSection();

            SolverSection();

            HandsSection();

            HelperSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void WarningsSection()
        {
            // Timestep
            if (Time.fixedDeltaTime > RECOMMENDED_TIMESTEP)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"Project timestep is larger than {RECOMMENDED_TIMESTEP} ({Time.fixedDeltaTime}). It is highly recommended to decrease this. " +
                    $"This will significantly improve the responsiveness of all physics calculations.", MessageType.Warning);
                if (GUILayout.Button("Fix Now", GUILayout.Width(80)))
                {
                    Time.fixedDeltaTime = RECOMMENDED_TIMESTEP;
                }
                EditorGUILayout.EndHorizontal();
            }
            // Solver iterations
            if (Physics.defaultSolverIterations < RECOMMENDED_SOLVER_ITERATIONS)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"Project solver iterations are lower than {RECOMMENDED_SOLVER_ITERATIONS} ({Physics.defaultSolverIterations}). It is highly recommended to increase this. " +
                    $"Hands will not be directly affected by this, but all other objects in your scene will be.", MessageType.Warning);
                if (GUILayout.Button("Fix Now", GUILayout.Width(80)))
                {
                    Physics.defaultSolverIterations = RECOMMENDED_SOLVER_ITERATIONS;
                }
                EditorGUILayout.EndHorizontal();
            }
            // Solver iterations
            if (Physics.defaultSolverVelocityIterations < RECOMMENDED_SOLVER_VELOCITY_ITERATIONS)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"Project solver velocity iterations are lower than {RECOMMENDED_SOLVER_VELOCITY_ITERATIONS} ({Physics.defaultSolverVelocityIterations}). It is highly recommended to increase this. " +
                    $"Hands will not be directly affected by this, but all other objects in your scene will be.", MessageType.Warning);
                if (GUILayout.Button("Fix Now", GUILayout.Width(80)))
                {
                    Physics.defaultSolverVelocityIterations = RECOMMENDED_SOLVER_VELOCITY_ITERATIONS;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void SetupSection()
        {
            _inputProvider.isExpanded = !EditorGUILayout.BeginFoldoutHeaderGroup(!_inputProvider.isExpanded, "Provider Setup");
            // Inverted so it's open by default
            if (!_inputProvider.isExpanded)
            {
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
            EditorGUILayout.EndFoldoutHeaderGroup();
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
            EditorGUILayout.ObjectField($"{hand.Handedness} Hand", hand, typeof(PhysicsHand), true);
            EditorGUI.EndDisabledGroup();
        }

        private void PhysicsSection()
        {
            EditorGUILayout.Space();
            _automaticHandsLayer.isExpanded = !EditorGUILayout.BeginFoldoutHeaderGroup(!_automaticHandsLayer.isExpanded, _physicsLayersText);
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (!_automaticHandsLayer.isExpanded)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("Object Layers", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_defaultLayer);
                EditorGUILayout.PropertyField(_interactableLayers);
                EditorGUILayout.PropertyField(_noContactLayers);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Hand Layers", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;

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
                EditorGUI.indentLevel--;
            }
        }

        private void HandsSection()
        {
            EditorGUILayout.Space();
            _interHandCollisions.isExpanded = !EditorGUILayout.BeginFoldoutHeaderGroup(!_interHandCollisions.isExpanded, "Physics Hands Settings");
            // Hide by default
            if (!_interHandCollisions.isExpanded)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(_interHandCollisions);
                EditorGUILayout.PropertyField(_strength);
                EditorGUILayout.PropertyField(_perBoneMass);
                EditorGUILayout.PropertyField(_handTeleportDistance);
                EditorGUILayout.PropertyField(_handGraspTeleportDistance);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void SolverSection()
        {
            EditorGUILayout.Space();
            _handSolverIterations.isExpanded = !EditorGUILayout.BeginFoldoutHeaderGroup(!_handSolverIterations.isExpanded, "Physics Settings");
            if (!_handSolverIterations.isExpanded)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                Time.fixedDeltaTime = EditorGUILayout.FloatField("Physics Timestep", Time.fixedDeltaTime);
                EditorGUILayout.LabelField($"Physics Updates Per Second: ~{(int)(1.0f / Time.fixedDeltaTime)}");

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Project Solver", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                Physics.defaultSolverIterations = EditorGUILayout.IntField(_projectSolverText, Physics.defaultSolverIterations);
                Physics.defaultSolverVelocityIterations = EditorGUILayout.IntField(_projectSolverVelocityText, Physics.defaultSolverVelocityIterations);
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Hand Solver", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                ValidateSolver();
                int oldPreset = _currentPreset;
                _currentPreset = EditorGUILayout.Popup(_solverPresetText, _currentPreset, HAND_SOLVER_NAMES);
                if (oldPreset != _currentPreset && _currentPreset < HAND_SOLVER_NAMES.Length - 1)
                {
                    ApplySolverSettings();
                }
                EditorGUILayout.PropertyField(_handSolverIterations);
                EditorGUILayout.PropertyField(_handSolverVelocityIterations);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void ValidateSolver()
        {
            int indA = Array.IndexOf(HAND_SOLVER_ITERATIONS, _handSolverIterations.intValue);
            int indB = Array.IndexOf(HAND_SOLVER_VELOCITY_ITERATIONS, _handSolverVelocityIterations.intValue);
            if (indA != indB)
            {
                _currentPreset = HAND_SOLVER_NAMES.Length - 1;
            }
            else
            {
                _currentPreset = indA;
            }
        }

        private void ApplySolverSettings()
        {
            _handSolverIterations.intValue = HAND_SOLVER_ITERATIONS[_currentPreset];
            _handSolverVelocityIterations.intValue = HAND_SOLVER_VELOCITY_ITERATIONS[_currentPreset];
        }

        private void HelperSection()
        {
            EditorGUILayout.Space();
            _enableHelpers.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(_enableHelpers.isExpanded, "Helpers");
            // Hide by default
            if (_enableHelpers.isExpanded)
            {
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
                    EditorGUILayout.PropertyField(_enhanceThrowing);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}