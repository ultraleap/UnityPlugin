/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
            if (_rigid != null && _physicsProvider != null)
            {
                _text.text = _prefix + (_physicsProvider.IsGraspingObject(_rigid) ? "Yes" : "No");
            }
        }
    }
}