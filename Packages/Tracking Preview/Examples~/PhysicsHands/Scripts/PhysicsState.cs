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
            _rigid = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            if (_physicsProvider == null)
            {
                _physicsProvider = FindObjectOfType<PhysicsProvider>(true);
            }
        }

        private void FixedUpdate()
        {
            if (_physicsProvider != null)
            {
                if (_physicsProvider.IsObjectHovered(_rigid))
                {
                    if (_physicsProvider.GetObjectState(_rigid, out var state))
                    {
                        _text.text = _prefix + state.ToString();
                    }
                }
                else
                {
                    _text.text = _prefix + "Idle";
                }
            }
        }
    }
}