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
    /// It is designed for use when you quickly want events for a single rigidbody only.
    /// Please reference PhysicsInterfaces.cs for more information.
    /// </summary>
    public class PhysicsGrasped : MonoBehaviour, IPhysicsHandGrab
    {
        private TextMeshPro _text;

        [SerializeField]
        private string _prefix = "Object Grasped?\n";

        private HashSet<PhysicsHand> _grabbedHands = new HashSet<PhysicsHand>();

        private void Start()
        {
            _text = GetComponentInChildren<TextMeshPro>(true);
        }

        public void OnHandGrab(PhysicsHand hand)
        {
            _grabbedHands.Add(hand);
            UpdateText();
        }

        public void OnHandGrabExit(PhysicsHand hand)
        {
            _grabbedHands.Remove(hand);
            UpdateText();
        }

        private void UpdateText()
        {
            if (_grabbedHands.Count > 0)
            {
                _text.text = _prefix + "Yes";
            }
        }
    }
}