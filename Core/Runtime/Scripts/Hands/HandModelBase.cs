/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
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
    public enum Chirality { Left, Right };
    public enum ModelType { Graphics, Physics };

    [ExecuteInEditMode]
    public abstract class HandModelBase : MonoBehaviour
    {
        public event Action OnBegin;
        public event Action OnFinish;
        /// <summary> Called directly after the HandModelBase's UpdateHand().
        /// </summary>
        public event Action OnUpdate;

        private bool init = false;

        private bool isTracked = false;
        public bool IsTracked
        {
            get { return isTracked; }
        }

        public abstract Chirality Handedness { get; set; }
        public abstract ModelType HandModelType { get; }
        public virtual void InitHand() { }

        public virtual void BeginHand()
        {
            if (OnBegin != null)
            {
                OnBegin();
            }
            isTracked = true;
        }
        public abstract void UpdateHand();
        public void UpdateHandWithEvent()
        {
            UpdateHand();
            if (OnUpdate != null)
            {
                OnUpdate();
            }
        }
        public virtual void FinishHand()
        {
            if (OnFinish != null)
            {
                OnFinish();
            }
            isTracked = false;
        }
        public abstract Hand GetLeapHand();
        public abstract void SetLeapHand(Hand hand);

        /// <summary>
        /// Returns whether or not this hand model supports editor persistence.  This is false by default and must be
        /// opt-in by a developer making their own hand model script if they want editor persistence.
        /// </summary>
        public virtual bool SupportsEditorPersistence()
        {
            return false;
        }

        [Tooltip("Optionally set a Leap Provider to use for tracking frames  \n" +
        "If you do not set one, the first provider found in the scene will be used. \n" +
        "If no provider is found this gameobject will disable itself")]
        public LeapProvider leapProvider;


        private void Awake()
        {
            if(!Application.isPlaying)
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
                    UpdateHand();
                }
            }
        }

#if UNITY_EDITOR

        //Only Runs in editor
        private void Update()
        {
            if (!Application.isPlaying && SupportsEditorPersistence())
            {
                //Try to set the provider for the user
                var Provider = Hands.Provider;
                Hand hand = null;
                if (Provider == null)
                {
                    //If we still have a null hand, construct one manually
                    if (hand == null)
                    {
                        hand = TestHandFactory.MakeTestHand(Handedness == Chirality.Left, unitType: TestHandFactory.UnitType.LeapUnits);
                        hand.Transform(transform.GetLeapMatrix());
                    }
                }
                else
                {
                    hand = Provider.CurrentFrame.GetHand(Handedness);
                }

                UpdateBase(hand);
            }
        }
#endif
    }
}
