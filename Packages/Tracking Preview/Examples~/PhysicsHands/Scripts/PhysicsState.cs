using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Leap.Unity.Interaction.PhysicsHands.Example
{
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
            if(_physicsProvider == null)
            {
                _physicsProvider = FindObjectOfType<PhysicsProvider>(true);
            }
            if(_physicsProvider != null)
            {
                _physicsProvider.OnObjectStateChange -= OnObjectStateChanged;
                _physicsProvider.OnObjectStateChange += OnObjectStateChanged;
            }
        }

        private void OnDisable()
        {
            if(_physicsProvider != null)
            {
                _physicsProvider.OnObjectStateChange -= OnObjectStateChanged;
            }
        }


        private void OnObjectStateChanged(Rigidbody rigid, PhysicsGraspHelper helper)
        {
            if(rigid == _rigid)
            {
                _text.text = _prefix + helper.GraspState.ToString();
            }
        }

    }
}