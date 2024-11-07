/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.XR;
#endif

namespace Leap.Controllers
{
    /// <summary>
    /// InputCheckBase is the base class for all InputChecks.
    /// This is used as a base class to compare Leap Hand Input and XRController Input.
    /// It also allows you to add a time threshold to your check.
    /// </summary>
    [System.Serializable]
    public class InputCheckBase
    {
        public bool enabled = true;
        public bool useTime = false;
        public InputMethodType inputMethodType = InputMethodType.LeapHand;
        public float timeThreshold = 0;
        public float TimeValue { get { return useTime ? timeThreshold : 0; } }
        public float actionThreshold = 0;
        public Chirality hand = Chirality.Left;

        protected LeapProvider _provider;
        protected float _currentTime = 0;

#if ENABLE_INPUT_SYSTEM
        protected XRController _xrController;
#endif

        public virtual void Setup(LeapProvider originalProvider)
        {
            _provider = originalProvider;
            _currentTime = 0;
            GetController();
        }

        public bool IsTrue()
        {
            if (IsTrueLogic())
            {
                _currentTime += Time.deltaTime;
                if (_currentTime > TimeValue)
                {
                    return true;
                }
                return false;
            }
            _currentTime = 0;
            return false;
        }

        protected virtual bool IsTrueLogic()
        {
            return true;
        }

        protected bool GetController()
        {
#if ENABLE_INPUT_SYSTEM

            if (_xrController == null)
            {
                switch (hand)
                {
                    case Chirality.Left:
                        _xrController = XRController.leftHand;
                        break;
                    case Chirality.Right:
                        _xrController = XRController.rightHand;
                        break;
                }
            }
            if (_xrController == null)
                return false;

            return _xrController.IsControllerActive();
#else
            return true;
#endif
        }

        public virtual void Reset()
        {
            _currentTime = 0;
        }
    }
}