/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

/** HandModelBase defines abstract methods as a template for building Leap hand models*/
namespace Leap.Unity
{
    using Attributes;

    /// <summary>
    /// Supported chiralities
    /// </summary>
    public enum Chirality { Left, Right };
    /// <summary>
    /// Supported hand model types
    /// </summary>
    public enum ModelType { Graphics, Physics };

    /// <summary>
    /// HandModelBase defines abstract methods as a template for building hand models.
    /// It allows you to receive tracking data for a left or right hand and it can be
    /// extended to drive hand visuals or physics representations of hands.
    /// 
    /// A HandModelBase will automatically subscribe and receive tracking data 
    /// if there is a provider in the scene. The provider can be overridden (leapProvider).
    /// </summary>
    [ExecuteInEditMode]
    public abstract class HandModelBase : MonoBehaviour
    {
        /// <summary>
        /// Called directly after HandModelBase's InitHand().
        /// </summary>
        public event Action OnBegin;
        /// <summary>
        /// Called when hand is not detected anymore in the current frame.
        /// </summary>
        public event Action OnFinish;
        /// <summary> 
        /// Called when hand model is active and updated with new data.
        /// </summary>
        public event Action OnUpdate;

        private bool init = false;

        private bool isTracked = false;
        /// <summary>
        /// Reports whether the hand is detected and tracked in the current frame.
        /// It is set to true after the event OnBegin and set to false after the event OnFinish
        /// </summary>
        public bool IsTracked
        {
            get { return isTracked; }
        }

        /// <summary>
        /// The chirality or handedness of this hand (left or right).
        /// </summary>
        public abstract Chirality Handedness { get; set; }
        /// <summary>
        /// The type of the Hand model (graphics or physics).
        /// </summary>
        public abstract ModelType HandModelType { get; }
        /// <summary>
        /// Implement this function to initialise this hand after it is created.
        /// This function is called when a new hand is detected by the Tracking Service.
        /// </summary>
        public virtual void InitHand() { }
        /// <summary>
        /// Called after hand is initialised. 
        /// Calls the event OnBegin and sets isTracked to true.
        /// </summary>
        public virtual void BeginHand()
        {
            if (OnBegin != null)
            {
                OnBegin();
            }
            isTracked = true;
        }
        /// <summary>
        /// Called once per frame when the LeapProvider calls the event 
        /// OnUpdateFrame (graphics hand) or OnFixedFrame (physics hand)
        /// </summary>
        public abstract void UpdateHand();

        /// <summary>
        /// Calls UpdateHand() and calls the event OnUpdate.
        /// </summary>
        public void UpdateHandWithEvent()
        {
            UpdateHand();
            if (OnUpdate != null)
            {
                OnUpdate();
            }
        }
        /// <summary>
        /// Called when the hand is not detected anymore in the current frame.
        /// Calls the event OnFinish and sets isTracked to false.
        /// </summary>
        public virtual void FinishHand()
        {
            if (OnFinish != null)
            {
                OnFinish();
            }
            isTracked = false;
        }
        /// <summary>
        /// Returns the Leap Hand object represented by this HandModelBase. 
        /// Note that any physical quantities and directions obtained from the Leap Hand object are 
        /// relative to the Leap coordinate system, which uses a right-handed axes and units 
        /// of millimeters.
        /// </summary>
        /// <returns></returns>
        public abstract Hand GetLeapHand();
        /// <summary>
        /// Assigns a Leap Hand object to this HandModelBase.
        /// </summary>
        /// <param name="hand"></param>
        public abstract void SetLeapHand(Hand hand);

        /// <summary>
        /// Returns whether or not this hand model supports editor persistence.  This is false by default and must be
        /// opt-in by a developer making their own hand model script if they want editor persistence.
        /// </summary>
        public virtual bool SupportsEditorPersistence()
        {
            return false;
        }

        [Tooltip("Optionally set a Leap Provider to use for tracking frames, If you do not set one, the first provider found in the scene will be used. If no provider is found this gameobject will disable itself")]
        [SerializeField]
        [EditTimeOnly]
        private LeapProvider _leapProvider;

        /// <summary>
        /// Optionally set a Leap Provider to use for tracking frames.
        /// If you do not set one, the first provider found in the scene will be used.
        /// If no provider is found this gameobject will disable itself.
        /// </summary>

        public LeapProvider leapProvider
        {
            get { return _leapProvider; }
            set
            {
                if (_leapProvider != null && Application.isPlaying)
                {
                    leapProvider.OnUpdateFrame -= UpdateFrame;
                    leapProvider.OnFixedFrame -= FixedUpdateFrame;
                }

                _leapProvider = value;

                if (_leapProvider != null && Application.isPlaying)
                {
                    if (HandModelType == ModelType.Graphics)
                    {
                        leapProvider.OnUpdateFrame -= UpdateFrame;
                        leapProvider.OnUpdateFrame += UpdateFrame;
                    }
                    else
                    {
                        leapProvider.OnFixedFrame -= FixedUpdateFrame;
                        leapProvider.OnFixedFrame += FixedUpdateFrame;
                    }


                }
            }
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            init = false;

            if (leapProvider == null)
            {
                //Try to set the provider for the user
                leapProvider = Hands.Provider;

                if (leapProvider == null)
                {
                    Debug.LogError("No leap provider found in the scene, hand model has been disabled", this.gameObject);
                    this.enabled = false;
                    this.gameObject.SetActive(false);
                    return;
                }
            }

            if (HandModelType == ModelType.Graphics)
            {
                leapProvider.OnUpdateFrame -= UpdateFrame;
                leapProvider.OnUpdateFrame += UpdateFrame;
            }

            else
            {
                leapProvider.OnFixedFrame -= FixedUpdateFrame;
                leapProvider.OnFixedFrame += FixedUpdateFrame;
            }

        }

        private void OnDestroy()
        {
            if (leapProvider == null) { return; }

            leapProvider.OnUpdateFrame -= UpdateFrame;
            leapProvider.OnFixedFrame -= FixedUpdateFrame;

#if UNITY_EDITOR
            Update();
#endif
        }

        void UpdateFrame(Frame frame)
        {
            if (this == null)
            {
                leapProvider.OnUpdateFrame -= UpdateFrame;
                return;
            }

            var hand = frame.GetHand(Handedness);
            UpdateBase(hand);
        }

        void FixedUpdateFrame(Frame frame)
        {
            var hand = frame.GetHand(Handedness);
            UpdateBase(hand);
        }

        void UpdateBase(Hand hand)
        {
            SetLeapHand(hand);

            if (hand == null)
            {
                if (IsTracked)
                {
                    FinishHand();
                }
            }
            else
            {
                if (!IsTracked)
                {
                    if (!init)
                    {
                        InitHand();
                        init = true;
                    }

                    BeginHand();
                }

                if (gameObject.activeInHierarchy)
                {
                    UpdateHandWithEvent();
                }
            }
        }

#if UNITY_EDITOR

        //Only Runs in editor
        private void Update()
        {
            if (!Application.isPlaying && SupportsEditorPersistence())
            {
                Hand hand = null;

                if (leapProvider == null)
                {
                    //Try to set the provider for the user
                    leapProvider = Hands.Provider;

                    if (leapProvider == null)
                    {
                        //Construct a hand manually
                        hand = TestHandFactory.MakeTestHand(Handedness == Chirality.Left);
                    }
                    else
                    {
                        hand = leapProvider.CurrentFrame.GetHand(Handedness);
                    }
                }
                else
                {
                    hand = leapProvider.CurrentFrame.GetHand(Handedness);
                }

                UpdateBase(hand);
            }
        }
#endif
    }
}