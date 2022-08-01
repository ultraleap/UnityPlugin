using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Leap.Unity.Interaction.PhysicsHands.Example
{
    /// <summary>
    /// Example script to test whether an object is being grasped or not.
    /// This function helps you ensure you're doing something when the user is or isn't grasping your object.
    /// </summary>
    public class PhysicsGrasped : MonoBehaviour
    {
        private TextMeshPro _text;
        private Rigidbody _rigid;

        [SerializeField]
        private string _prefix = "Object Grasped?\n";

        private PhysicsProvider _physicsProvider;

        private void Start()
        {
            _text = GetComponentInChildren<TextMeshPro>(true);
            _rigid = GetComponent<Rigidbody>();
            _physicsProvider = FindObjectOfType<PhysicsProvider>(true);
        }

        private void FixedUpdate()
        {
            if(_rigid != null && _physicsProvider != null)
            {
                _text.text = _prefix + (_physicsProvider.IsGraspingObject(_rigid) ? "Yes" : "No");
            }   
        }
    }
}