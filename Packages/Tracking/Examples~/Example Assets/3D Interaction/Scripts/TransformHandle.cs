/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.InteractionEngine.Examples
{

    [AddComponentMenu("")]
    [RequireComponent(typeof(InteractionBehaviour))]
    public class TransformHandle : MonoBehaviour
    {

        protected InteractionBehaviour _intObj;
        protected TransformTool _tool;

        public UnityEvent OnShouldShowHandle = new UnityEvent();
        public UnityEvent OnShouldHideHandle = new UnityEvent();
        public UnityEvent OnHandleActivated = new UnityEvent();
        public UnityEvent OnHandleDeactivated = new UnityEvent();

        protected virtual void Start()
        {
            _intObj = GetComponent<InteractionBehaviour>();
            _intObj.OnGraspBegin += onGraspBegin;
            _intObj.OnGraspEnd += onGraspEnd;

            _tool = GetComponentInParent<TransformTool>();
            if (_tool == null)
                Debug.LogError("No TransformTool found in a parent GameObject.");
        }

        public void syncRigidbodyWithTransform()
        {
            _intObj.rigidbody.position = this.transform.position;
            _intObj.rigidbody.rotation = this.transform.rotation;
        }

        private void onGraspBegin()
        {
            _tool.NotifyHandleActivated(this);

            OnHandleActivated.Invoke();
        }

        private void onGraspEnd()
        {
            _tool.NotifyHandleDeactivated(this);

            OnHandleDeactivated.Invoke();
        }

        #region Handle Visibility

        /// <summary>
        /// Called by the Transform Tool when this handle should be visible.
        /// </summary>
        public void EnsureVisible()
        {
            OnShouldShowHandle.Invoke();

            _intObj.ignoreGrasping = false;
        }

        /// <summary>
        /// Called by the Transform Tool when this handle should not be visible.
        /// </summary>
        public void EnsureHidden()
        {
            OnShouldHideHandle.Invoke();

            _intObj.ignoreGrasping = true;
        }

        #endregion

    }

}