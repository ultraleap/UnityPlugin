/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Attachments;
using System;
using UnityEngine;

namespace Leap.Preview.Locomotion
{
    /// <summary>
    /// Used in conjunction with JumpGemTeleport, a Jump Gem can be pinched, aimed and released in order to teleport
    /// </summary>
    [RequireComponent(typeof(PinchDetector))]
    public class JumpGem : MonoBehaviour
    {
        #region Editor Settings

        public LeapProvider leapProvider;

        [SerializeField, Header("Pinch")]
        protected PinchDetector _pinchDetector = null;
        public PinchDetector PinchDetector => _pinchDetector;

        [SerializeField, Tooltip("This dictates the required pinch value and overall size of the visual elements used. " +
            "This value is a radius and will be used *0.5 on visual items.")]
        protected float _gemSize = 0.033f;

        [SerializeField, Tooltip("This will control the size of the visual elements when pinched. " +
            "This value is a radius and will be used *0.5 on visual items.")]
        protected float _gemSizeWhenPinched = 0.02f;

        [SerializeField, Range(0.001f, 2f), Tooltip("Scales the threshold of the required pinch distance based of the gem size. " +
            "This will change the pinch de-activation distance.")]
        protected float _pinchScale = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("The amount into the \"pinched state\" that the user has to pinch before it will be classified as a valid pinch.")]
        protected float _requiredPinchSquishAmount = 0.2f;

        [SerializeField, Header("Visuals"), Tooltip("The visual item you want to represent as the gem.")]
        protected Transform _pinchItem = null;

        [SerializeField, Tooltip("The visual item you want to represent where the gem originally came from. This will transition in when the gem moves further away during a pinch.")]
        protected Transform _outlineItem = null;

        [SerializeField, Tooltip("The keyword within the shader to use when changing the color value of the outline shader.")]
        protected string _outlineShaderKeyword = "_OutlineColor";

        [SerializeField, Tooltip("Hides the gem unless this object's blue axis is facing the user.")]
        protected bool _onlyShowWhenFacingUser = true;

        [SerializeField, Tooltip("The angle to rotate the pinched object by relative to the ray direction, when pinched.")]
        protected Vector3 _angleToRotateWhenPinched = new Vector3(90, 0, 0);

        [SerializeField, Tooltip("Used for colouring a visual aid as it transitions from its idle state to a valid and active one.")]
        protected Gradient _transitionGradient = null;

        [SerializeField, Tooltip("Decides whether the above gradient will also affect the emission value of the material.")]
        private bool _gradientControlsEmission = true;

        [SerializeField, Tooltip("Controls the vibrancy of the emission amount."), Range(0, 1f)]
        private float _gradientEmissionAmount = 0.5f;

        [SerializeField, Header("Audio"), Tooltip("Audio source on the gem to use for all of the specified clips below.")]
        private AudioSource _audioSource = null;

        [SerializeField, Tooltip("Audio clip to play when the gem is pinched.")]
        protected AudioClip _pinchClip;
        [SerializeField, Tooltip("Audio clip to play when the teleportation ray is shown.")]
        protected AudioClip _showClip;
        [SerializeField, Tooltip("Audio clip to play when the user successfully teleports.")]
        protected AudioClip _teleportClip;
        [SerializeField, Tooltip("Audio clip to play when the user fails to teleport.")]
        protected AudioClip _errorClip;

        #endregion

        #region Unity/Data Context

        protected Transform _head;

        protected Hand _attachedHand = null;
        protected Hand _pinchedHand = null;
        protected Chirality _pinchedChirality;
        public Chirality PinchedHand => _pinchedChirality;

        protected JumpGemTeleport _jumpGemTeleport;

        protected AttachmentHand _attachmentHand;

        protected MeshRenderer _meshRenderer, _outlineRenderer;
        protected bool _outlineShaderKeywordFound = false;

        protected bool _forcedHidden = false, _detectedHidden = false;

        public Transform PinchItem => _pinchItem;
        public Transform OutlineItem => _outlineItem;

        #endregion

        #region Interaction Information 

        protected float _scaledGemSize { get { return _gemSize * _pinchScale; } }

        protected bool _wasPinched = false, _isTeleporting = false;
        private bool _isPinched = false;
        public bool IsPinched => _isPinched;
        public bool IsGemHidden { get { return _forcedHidden || _detectedHidden || _facingAmount < 0.5f; } }

        private bool _hasPinchedEnough = false;
        public bool HasPinchedEnough => _hasPinchedEnough;

        protected Vector3 _pinchStartPosition;
        protected Quaternion _pinchStartRotation;
        protected Quaternion _rayRotation;
        protected Vector3 _releasePosition;

        protected float _facingAmount = 0f;
        protected bool _hasMovedAway = false;

        #endregion

        #region Interpolations

        // Allows the user to keep the pinch when they bring their hand back into frame
        protected float _nullTimeBack = 0.07f;
        protected float _nullTimeCurrent = 0f;

        // Ensures that we don't instantly fire release event when the hand drops tracking
        protected float _releaseTime = 0.07f;
        protected float _releaseTimeCurrent = 0f;

        // Pinch amount is the raw value reported by pinching/squeezing
        protected float _pinchAmount = 0, _currentPinchAmount = 0;
        protected float _pinchLerpTime = 0.1f;

        // Used to transition the visual elements in and out when they are no longer reported as visible by IsGemHidden
        protected float _hiddenTime = 0.15f;
        protected float _hiddenCurrent = 0, _hiddenOutlineCurrent = 0;

        // Transitions the gem from the original position and rotation to the pinch position
        protected float _pinchStartTime = 0.1f;
        protected float _pinchStartTimeCurrent = 0f;

        // Containers for current lerp values used by the visuals
        protected float _pinchSizeLerp, _distanceLerp, _distanceSizeLerp;

        #endregion

        /// <summary>
        /// Fired when the gem is initially pinched. Will return the chirality of the hand used to pinch it.
        /// </summary>
        public Action<Chirality> OnPinch;
        /// <summary>
        /// Fired when the gem is released. Will return true if all pre-requisites were met.
        /// </summary>
        public Action<bool> OnRelease;

        private void Awake()
        {
            if (_pinchItem == null)
            {
                Debug.LogError($"Pinch item has not been set for Gem {name} and will be disabled.");
                enabled = false;
                return;
            }

            if (leapProvider == null)
            {
                leapProvider = Hands.Provider;
            }
            _jumpGemTeleport = FindAnyObjectByType<JumpGemTeleport>(FindObjectsInactive.Include);

            if (_audioSource == null)
            {
                _audioSource = GetComponentInChildren<AudioSource>(true);
            }

            _attachmentHand = GetComponentInParent<AttachmentHand>();
            _head = Camera.main.transform;

            _meshRenderer = _pinchItem.GetComponentInChildren<MeshRenderer>(true);
            if (_meshRenderer != null && _meshRenderer.materials.Length > 0 && _meshRenderer.materials[0] != null)
            {
                _meshRenderer.materials[0] = new Material(_meshRenderer.materials[0]);
            }

            if (_outlineItem != null)
            {
                _outlineRenderer = _outlineItem.GetComponentInChildren<MeshRenderer>(true);
                if (_outlineRenderer != null && _outlineRenderer.materials.Length > 0 && _outlineRenderer.materials[0] != null)
                {
                    _outlineRenderer.materials[0] = new Material(_outlineRenderer.materials[0]);
                    _outlineShaderKeywordFound = _outlineRenderer.materials[0].HasProperty(_outlineShaderKeyword);
                }
            }
        }

        private void Update()
        {
            if (_attachmentHand != null)
            {
                _attachedHand = leapProvider.CurrentFrame.GetHand(_attachmentHand.chirality);
            }

            if (_isPinched)
            {
                PinchedGem();
            }
            else
            {
                _detectedHidden = !ValidateIdleGem();
                if (!_detectedHidden)
                {
                    DetectPinchingHand();
                }
            }

            ProcessGemValues();
            PositionGem();
            UpdateGemVisual();
        }

        #region Idle Logic

        /// <summary>
        /// Detect whether the idle gem is in a valid state to be pinched
        /// </summary>
        private bool ValidateIdleGem()
        {
            if (_forcedHidden)
            {
                return false;
            }

            if (_attachmentHand != null)
            {
                if (_attachedHand == null)
                {
                    return false;
                }
            }

            if (!gameObject.activeInHierarchy)
            {
                return false;
            }

            if (_onlyShowWhenFacingUser)
            {
                _facingAmount = Vector3.Dot(transform.forward, (_head.position - transform.position).normalized);
            }
            else
            {
                _facingAmount = 1;
            }

            return true;
        }

        /// <summary>
        /// Detect whether the gem should be pinched, and by which hand
        /// </summary>
        private void DetectPinchingHand()
        {
            Chirality chirality;
            Vector3 position;
            if (GetClosestHand(out chirality, out position))
            {
                if (chirality != _pinchDetector.chirality)
                {
                    _pinchDetector.chirality = chirality;
                    _wasPinched = false;
                }
            }

            if (_wasPinched && _pinchDetector.IsDoingAction)
            {
                return;
            }

            if (_currentPinchAmount > 0.2f)
            {
                return;
            }

            _wasPinched = _pinchDetector.IsDoingAction;

            if (!_wasPinched)
            {
                return;
            }

            if (ValidateDistanceBetweenGem(position))
            {
                PinchJewel(chirality);
            }
        }

        /// <summary>
        /// Checks to see which hand is closest. Returning false means that there is no hand present.
        /// </summary>
        private bool GetClosestHand(out Chirality chirality, out Vector3 pinchPosition)
        {
            chirality = Chirality.Left;
            pinchPosition = Vector3.zero;
            Hand left = leapProvider.CurrentFrame.GetHand(Chirality.Left);
            Hand right = leapProvider.CurrentFrame.GetHand(Chirality.Right);
            if (left == null && right == null)
            {
                return false;
            }

            float leftDist = 100, rightDist = 100;
            Vector3 leftPos = Vector3.zero, rightPos = Vector3.zero;
            if (left != null)
            {
                leftPos = left.GetPredictedPinchPosition();
                leftDist = Vector3.Distance(transform.position, leftPos);
            }
            if (right != null)
            {
                rightPos = right.GetPredictedPinchPosition();
                rightDist = Vector3.Distance(transform.position, rightPos);
            }
            chirality = leftDist < rightDist ? Chirality.Left : Chirality.Right;
            pinchPosition = leftDist < rightDist ? leftPos : rightPos;

            return true;
        }

        /// <summary>
        /// Ensure the pinching hand is within a suitable distance to the gem.
        /// </summary>
        private bool ValidateDistanceBetweenGem(Vector3 position)
        {
            return Vector3.Distance(position, _pinchItem.position) <= _scaledGemSize;
        }

        private void PinchJewel(Chirality chirality)
        {
            _releaseTimeCurrent = _releaseTime;
            _pinchStartTimeCurrent = _pinchStartTime;
            _isPinched = true;
            _hasMovedAway = false;
            _pinchedChirality = chirality;
            _pinchStartPosition = _pinchItem.position;
            _pinchStartRotation = _pinchItem.rotation;
            _pinchItem.transform.SetParent(null);

            PlaySound(_pinchClip);
            OnPinch?.Invoke(chirality);
        }

        #endregion

        #region Pinching Logic

        /// <summary>
        /// Logic flow for when the gem is reported as pinched
        /// </summary>
        private void PinchedGem()
        {
            _facingAmount = 1f;
            _pinchedHand = leapProvider.CurrentFrame.GetHand(_pinchedChirality);
            _pinchAmount = 1f;
            if (_pinchedHand != null)
            {
                _detectedHidden = false;

                if (_nullTimeCurrent > 0f)
                {
                    _hasPinchedEnough = false;
                    _nullTimeCurrent -= Time.deltaTime;
                    return;
                }

                if (_pinchDetector.SquishPercent > _requiredPinchSquishAmount)
                {
                    _hasPinchedEnough = true;
                }

                if (!_pinchDetector.IsDoingAction)
                {
                    if (_releaseTimeCurrent > 0f)
                    {
                        _releaseTimeCurrent -= Time.deltaTime;
                        return;
                    }
                    _isPinched = false;
                    _pinchItem.transform.SetParent(transform);
                    _releasePosition = _pinchItem.localPosition;
                    if (_hasPinchedEnough && _jumpGemTeleport != null && _jumpGemTeleport.IsValid && _jumpGemTeleport.IsSelected)
                    {
                        PlaySound(_teleportClip);
                    }
                    else
                    {
                        PlaySound(_errorClip);
                    }
                    OnRelease?.Invoke(_hasPinchedEnough);
                    _hasPinchedEnough = false;
                    _wasPinched = true;
                    _pinchAmount = 0f;
                    _pinchedHand = null;
                }
                else
                {
                    _releaseTimeCurrent = _releaseTime;
                }
            }
            else
            {
                _detectedHidden = true;
                _nullTimeCurrent = _nullTimeBack;
            }
        }

        #endregion

        /// <summary>
        /// Move the gem into the correct position depending on current interaction state
        /// </summary>
        private void PositionGem()
        {
            if (_isPinched)
            {
                _rayRotation = Quaternion.LookRotation(_jumpGemTeleport.handRayInteractor.handRay.handRayDirection.Direction) * Quaternion.Euler(_angleToRotateWhenPinched);

                if (_pinchStartTimeCurrent > 0)
                {
                    _pinchStartTimeCurrent = Mathf.Lerp(_pinchStartTimeCurrent, 0, Time.deltaTime * (1.0f / _pinchStartTime));
                    if (_pinchStartTimeCurrent < 1e-5)
                    {
                        _pinchStartTimeCurrent = 0;
                    }
                }

                if (_pinchedHand != null)
                {
                    if (_pinchStartTimeCurrent > 0)
                    {
                        _pinchItem.position = Vector3.Lerp(_pinchStartPosition,
                            Vector3.Lerp(_pinchedHand.fingers[0].TipPosition, _pinchedHand.fingers[1].TipPosition, 0.5f),
                            Mathf.InverseLerp(_pinchStartTime, 0, _pinchStartTimeCurrent));
                        _pinchItem.rotation = Quaternion.Lerp(_pinchStartRotation,
                            _rayRotation,
                            Mathf.InverseLerp(_pinchStartTime, 0, _pinchStartTimeCurrent));
                    }
                    else
                    {
                        _pinchItem.position = Vector3.Lerp(_pinchedHand.fingers[0].TipPosition, _pinchedHand.fingers[1].TipPosition, 0.5f);
                        _pinchItem.rotation = _rayRotation;
                    }
                }
            }
            else
            {
                if (_currentPinchAmount > 0)
                {
                    _pinchItem.localPosition = Vector3.Lerp(Vector3.zero, _releasePosition, _currentPinchAmount);
                    _pinchItem.localRotation = Quaternion.Slerp(Quaternion.identity, transform.TransformRotation(_rayRotation), _currentPinchAmount);
                }
                else
                {
                    _pinchItem.localPosition = Vector3.zero;
                    _pinchItem.localRotation = Quaternion.identity;
                }
            }
            if (_outlineItem != null)
            {
                _outlineItem.localPosition = Vector3.zero;
                _outlineItem.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Manually enforce the gem to be hidden
        /// </summary>
        public void ChangeHidden(bool hidden)
        {
            _forcedHidden = hidden;
        }

        /// <summary>
        /// Calculate and process all the values that are then used by visuals or logic checks.
        /// </summary>
        private void ProcessGemValues()
        {
            // Have we lost tracking or need to hide the jewels?
            _hiddenCurrent += (IsGemHidden ? 1 : -1) * Time.deltaTime * (1.0f / _hiddenTime);
            _hiddenCurrent = Mathf.Clamp01(_hiddenCurrent);

            // Interpolate the pinch amount so the visuals are smooth
            _currentPinchAmount = Mathf.Lerp(_currentPinchAmount, _pinchAmount, Time.deltaTime * (1.0f / _pinchLerpTime));

            // Round down to zero to prevent excessive lerping time.
            if (_currentPinchAmount < 1e-4)
            {
                _currentPinchAmount = 0;
            }

            _pinchSizeLerp = Mathf.Lerp(_gemSize, _gemSizeWhenPinched, _currentPinchAmount) * 0.5f;
            _pinchSizeLerp *= 1 - _hiddenCurrent;

            _distanceLerp = Mathf.InverseLerp(0, _jumpGemTeleport.DistanceToActivate, DistanceFromPoint());

            _distanceSizeLerp = Mathf.Lerp(_gemSize, _gemSizeWhenPinched, _distanceLerp) * 0.5f;

            bool old = _hasMovedAway;
            if (_distanceLerp >= 1)
            {
                _hasMovedAway = true;
            }
            else
            {
                _hasMovedAway = false;
            }
            if (old != _hasMovedAway && _hasMovedAway)
            {
                PlaySound(_showClip);
            }
        }

        /// <summary>
        /// Update the visuals of the gem. This should happen after all processing has occurred so the visuals and data are in the correct step.
        /// </summary>
        protected virtual void UpdateGemVisual()
        {
            // Interpolate the sizes of the gem when pinched
            _pinchItem.localScale = new Vector3(_pinchSizeLerp, _gemSize * 0.5f * (1 - _hiddenCurrent), _pinchSizeLerp);

            // Interpolate the colors (and alpha of the outline) based on distance
            Color gradientColor = _transitionGradient.Evaluate(_distanceLerp);
            _meshRenderer.materials[0].color = gradientColor;

            // Set the emission of the material to the interpolated colour
            if (_gradientControlsEmission)
            {
                // Full emission colors are very bright so tone it down a bit
                float h, s, v;
                Color.RGBToHSV(gradientColor, out h, out s, out v);
                v *= _gradientEmissionAmount;
                gradientColor = Color.HSVToRGB(h, s, v);

                _meshRenderer.materials[0].SetColor("_EmissionColor", gradientColor);
                _meshRenderer.materials[0].EnableKeyword("_EMISSION");
            }

            // Adjust the colors of the outline item
            if (_outlineRenderer != null)
            {
                _outlineItem.localScale = new Vector3(_distanceSizeLerp, _distanceSizeLerp, _distanceSizeLerp);
                Color c = _transitionGradient.Evaluate(_distanceLerp);

                _hiddenOutlineCurrent += ((_attachmentHand != null && _attachedHand == null) ? 1 : -1) * Time.deltaTime * (1.0f / _hiddenTime);
                _hiddenOutlineCurrent = Mathf.Clamp01(_hiddenOutlineCurrent);

                c.a = _distanceLerp * (1 - _hiddenOutlineCurrent);
                if (_outlineShaderKeywordFound)
                {
                    _outlineRenderer.materials[0].SetColor(_outlineShaderKeyword, c);
                }
            }
        }

        /// <summary>
        /// Returns the distance between the gem's original position and the current pinched value.
        /// If the gem is not pinched then it will always return zero.
        /// </summary>
        public float DistanceFromPoint()
        {
            if (_isPinched)
            {
                if (_attachmentHand != null && _attachedHand == null)
                {
                    // Fall back to the head if the attached hand loses tracking
                    return Vector3.Distance(_pinchItem.position, _head.position);
                }
                return Vector3.Distance(_pinchItem.position, transform.position);
            }
            return 0;
        }

        /// <summary>
        /// Plays an audio event from the gem.
        /// </summary>
        protected void PlaySound(AudioClip clip)
        {
            if (clip == null || _audioSource == null)
                return;

            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
            _audioSource.PlayOneShot(clip);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_pinchItem != null)
            {
                _pinchItem.localScale = new Vector3(_gemSize, _gemSize, _gemSize) * 0.5f;
            }

            if (_outlineItem != null)
            {
                _outlineItem.localScale = new Vector3(_gemSize, _gemSize, _gemSize) * 0.5f;
            }

            if (_pinchDetector == null)
            {
                _pinchDetector = GetComponent<PinchDetector>();
            }
            if (_pinchDetector != null)
            {
                _pinchDetector.activateDistance = _gemSize;
                _pinchDetector.deactivateDistance = _scaledGemSize;
            }

            if (_audioSource == null)
            {
                _audioSource = GetComponentInChildren<AudioSource>();
            }
        }
#endif
    }
}