using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    // Use this script to show a subtle outline of an original data hand to help with perception.
    public class PhysicsGhostHand : MonoBehaviour
    {
        [SerializeField]
        private PhysicsHand _physicsHand;

        [SerializeField]
        private HandModelBase _handModel;

        [SerializeField]
        private float _alpha = 0.02f;

        [SerializeField]
        private float _minimumDistance = 0.02f, _maximumDistance = 0.08f;

        [SerializeField]
        private float _lerpTime = 0.1f;

        private Renderer _renderer;

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>(true);
            _renderer.material.SetColor("_MainColor", Color.clear);
        }

        void Update()
        {
            if (_physicsHand != null)
            {
                float val = 0;
                if (_physicsHand.IsGrasping)
                {
                    val = _physicsHand.GetOriginalLeapHand().GetFistStrength() * .25f;
                }
                if (val < _physicsHand.DistanceFromDataHand)
                {
                    val = _physicsHand.DistanceFromDataHand;
                }
                if (val < _minimumDistance)
                {
                    val = 0;
                }

                Color c = Color.white;
                c.a = Mathf.Lerp(_renderer.material.GetColor("_MainColor").a, Mathf.Lerp(0, _alpha, Mathf.InverseLerp(_minimumDistance, _maximumDistance, val)), Time.deltaTime / (1.0f * _lerpTime));
                _renderer.material.SetColor("_MainColor", c);
            }
        }

        private void OnValidate()
        {
            if (_handModel == null)
            {
                _handModel = GetComponent<HandModelBase>();
            }
            if (_handModel != null && _physicsHand == null)
            {
                _physicsHand = FindObjectsOfType<PhysicsHand>().Where(x => x.Handedness == _handModel.Handedness).DefaultIfEmpty(null).First();
            }
        }
    }
}