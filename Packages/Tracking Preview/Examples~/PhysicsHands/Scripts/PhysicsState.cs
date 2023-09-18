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
    /// Example script that listens to object state changes, fired by the Physics Provider.
    /// This event allows you to easily bind your own custom logic.
    /// It is designed for use when using either a specific or numerous rigidbodies.
    /// </summary>
    public class PhysicsState : MonoBehaviour
    {
        private TextMeshPro _text;
        private Rigidbody _rigid;

        [SerializeField]
        private string _prefix = "Object State: ";

        private PhysicsProvider _physicsProvider;

        private void Start()
        {
            _text = GetComponentInChildren<TextMeshPro>(true);
        }

        private void OnEnable()
        {
            if (_physicsProvider == null)
            {
                _physicsProvider = FindAnyObjectByType<PhysicsProvider>(FindObjectsInactive.Include);
            }
            if (_rigid == null)
            {
                _rigid = GetComponent<Rigidbody>();
            }
            if (_physicsProvider != null && _rigid != null)
            {
                _physicsProvider.SubscribeToStateChanges(_rigid, StateChange);
            }
        }

        private void OnDisable()
        {
            if (_physicsProvider != null)
            {
                _physicsProvider.UnsubscribeFromStateChanges(_rigid, StateChange);
            }
        }

        private void StateChange(PhysicsGraspHelper graspHelper)
        {
            if (graspHelper == null)
            {
                _text.text = _prefix + "Idle";
            }
            else
            {
                _text.text = _prefix + graspHelper.GraspState.ToString();
            }
        }
    }
}