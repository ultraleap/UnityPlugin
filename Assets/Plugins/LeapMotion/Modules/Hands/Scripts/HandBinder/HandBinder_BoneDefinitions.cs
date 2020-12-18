using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity
{
    [CreateAssetMenu(fileName = "AutoRigger_Definitions", menuName = "Ultraleap/Autorigger_Definitions", order = 1)]
    public class HandBinder_BoneDefinitions : ScriptableObject
    {
        public BoneDefinitions boneDefinitions = new BoneDefinitions();
    }
    /// <summary>
    /// Used to define what bones names are valid for each finger
    /// </summary>
    [System.Serializable]
    public class BoneDefinitions
    {
        public string[] _definition_Thumb = { "thumb" };
        public string[] _definition_Index = { "index" };
        public string[] _definition_Middle = { "middle" };
        public string[] _definition_Ring = { "ring" };
        public string[] _definition_Pinky = { "pinky", "little" };
        public string[] _definition_Wrist = { "wrist", "hand", "palm" };
        public string[] _definition_Elbow = { "elbow", "upperArm" };
    }
}
