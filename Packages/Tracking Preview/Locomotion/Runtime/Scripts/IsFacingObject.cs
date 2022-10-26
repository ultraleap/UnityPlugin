using Leap.Unity;
using Leap.Unity.Interaction;
using Leap.Unity.Interaction.PhysicsHands;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    public class IsFacingObject : MonoBehaviour
    {
        [Tooltip("Note that the Interaction Engine and Physics Hands layers will be ignored automatically.")]
        [Header("Layer Logic")]
        [SerializeField] private List<SingleLayer> _layersToIgnore = new List<SingleLayer>();

        protected LayerMask _layerMask;

        private bool hasCastThisFrame = false;

        private bool _valueThisFrame = false;

        [SerializeField] private float _sphereCastRadius = 0.3f;
        [SerializeField] private float _sphereCastMaxLength = 0.5f;
        [SerializeField] private string _teleportAnchorTag = "LEAP_TELEPORTER";
        [SerializeField] private string _teleportFloorTag = "LEAP_FLOOR";
        public bool ValueThisFrame
        {
            get
            {
                if (!hasCastThisFrame)
                {
                    CheckIfFacingObject();
                }
                return _valueThisFrame;
            }
        }

        void Start()
        {
            SetLayerMask();
        }

        private void SetLayerMask()
        {
            _layerMask = -1;
            InteractionManager interactionManager = FindObjectOfType<InteractionManager>();

            // Ignore any interaction objects 
            if (interactionManager != null)
            {
                // Ignore the user's hands - they are likely to be in front of the head, as it's the most common interaction zone
                _layerMask ^= interactionManager.contactBoneLayer.layerMask;
                // Ignore any grasped objects 
                _layerMask ^= interactionManager.interactionNoContactLayer.layerMask;
            }

            PhysicsProvider physicsProvider = FindObjectOfType<PhysicsProvider>();
            if (physicsProvider != null)
            {
                _layerMask ^= physicsProvider.HandsLayer.layerMask;
                _layerMask ^= physicsProvider.HandsResetLayer.layerMask;
            }

            foreach (var layers in _layersToIgnore)
            {
                _layerMask ^= layers.layerMask;
            }
        }

        private void CheckIfFacingObject()
        {
            Transform head = Camera.main.transform;
            RaycastHit hit;

            Debug.DrawRay(head.position, head.forward);
            if (Physics.SphereCast(new Ray(head.position, head.forward), _sphereCastRadius, out hit, _sphereCastMaxLength, _layerMask))
            {
                if (hit.transform.tag == _teleportFloorTag || hit.transform.tag == _teleportAnchorTag)
                {
                    _valueThisFrame = false;

                }
                else
                {
                    _valueThisFrame = true;
                }
            }
            else
            {
                _valueThisFrame = false;
            }
        }

        private void OnDrawGizmos()
        {
            //TODO - fix this spherecast visualisation
            //Gizmos.DrawWireSphere(transform.position, _sphereCastMaxLength);
            //Transform head = Camera.main.transform;

            //RaycastHit hit;
            //if (Physics.SphereCast(new Ray(head.position, head.forward), _sphereCastRadius, out hit, _sphereCastMaxLength, _layerMask))
            //{
            //    Gizmos.color = Color.green;
            //    Vector3 sphereCastMidpoint = transform.position + (transform.forward * hit.distance);
            //    Gizmos.DrawWireSphere(sphereCastMidpoint, _sphereCastRadius);
            //    Gizmos.DrawSphere(hit.point, 0.1f);
            //    Debug.DrawLine(transform.position, sphereCastMidpoint, Color.green);
            //}
            //else
            //{
            //    Gizmos.color = Color.red;
            //    Vector3 sphereCastMidpoint = transform.position + (transform.forward * (_sphereCastMaxLength - _sphereCastRadius));
            //    Gizmos.DrawWireSphere(sphereCastMidpoint, _sphereCastRadius);
            //    Debug.DrawLine(transform.position, sphereCastMidpoint, Color.red);
            //}
        }
    }
}
