/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity
{

    /// <summary>
    /// A component to be attached to a HandModelBase to handle starting and ending of
    /// tracking. `HandReset` is called when tracking begins. `HandFinish` is
    /// called when tracking ends.
    /// </summary>
    [RequireComponent(typeof(HandModelBase))]
    [DefaultExecutionOrder(1)]
    public abstract class HandTransitionBehavior : MonoBehaviour
    {
        [Tooltip("Should this hand GameObject begin disabled? \n" +
"You can use a Hand Enable Disable component to turn on and off this gameobject when a hand is detected and lost")]
        public bool disableOnAwake = true;

        protected HandModelBase handModelBase;

        protected abstract void HandReset();
        protected abstract void HandFinish();

        protected virtual void Awake()
        {
            handModelBase = GetComponent<HandModelBase>();

            handModelBase.OnBegin -= HandReset;
            handModelBase.OnBegin += HandReset;

            handModelBase.OnFinish -= HandFinish;
            handModelBase.OnFinish += HandFinish;

            this.gameObject.SetActive(disableOnAwake ? false : this.gameObject.activeInHierarchy);
        }

        protected virtual void OnDestroy()
        {
            if (handModelBase == null)
            {
                HandModelBase handModelBase = GetComponent<HandModelBase>();
                if (handModelBase == null)
                {
                    Debug.LogWarning("HandTransitionBehavior components require a HandModelBase "
                      + "component attached to the same GameObject. (OnDestroy)");
                    return;
                }
            }

            handModelBase.OnBegin -= HandReset;
            handModelBase.OnFinish -= HandFinish;
        }
    }
}