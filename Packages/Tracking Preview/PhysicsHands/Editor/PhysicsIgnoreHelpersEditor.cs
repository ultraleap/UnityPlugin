using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    [CustomEditor(typeof(PhysicsIgnoreHelpers)), CanEditMultipleObjects]
    public class PhysicsIgnoreHelpersEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This script will prevent the physics hands helpers from being applied to your object.\n" +
                "This allows you to easily prevent important objects from being affected by the player.\n" +
                "Note that this will not prevent your object from being collided with.", MessageType.Info);
        }
    }
}