/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/


using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.PhysicalHands
{

    [System.Serializable]
    public class PhysicalHandsAnchorSet : SerializableHashSet<PhysicalHandsAnchor> { }

    public class PhysicalHandsAnchorGroup : MonoBehaviour
    {

        [SerializeField]
        [Tooltip("The anchors that are within this AnchorGroup. Anchorable objects associated "
               + "this AnchorGroup can only be placed in anchors within this group.")]
        private PhysicalHandsAnchorSet _anchors = null;
        public PhysicalHandsAnchorSet anchors { get { return _anchors; } }

        private HashSet<PhysicalHandsAnchorableBehaviour> _anchorableObjects = new HashSet<PhysicalHandsAnchorableBehaviour>();
        /// <summary>
        /// Gets the PhysicalHandsAnchorableBehaviours that are set to this AnchorGroup.
        /// </summary>
        public HashSet<PhysicalHandsAnchorableBehaviour> anchorableObjects { get { return _anchorableObjects; } }

        void Awake()
        {
            foreach (var anchor in anchors)
            {
                Add(anchor);
            }
        }

        void OnDestroy()
        {
            foreach (var anchor in anchors)
            {
                anchor.groups.Remove(this);
            }
        }

        public bool Contains(PhysicalHandsAnchor anchor)
        {
            return _anchors.Contains(anchor);
        }

        public bool Add(PhysicalHandsAnchor anchor)
        {
            anchor.groups.Add(this);
            return _anchors.Add(anchor);
        }

        public bool Remove(PhysicalHandsAnchor anchor)
        {
            anchor.groups.Remove(this);
            return _anchors.Remove(anchor);
        }

        public void NotifyAnchorableObjectAdded(PhysicalHandsAnchorableBehaviour anchObj)
        {
            anchorableObjects.Add(anchObj);
        }

        public void NotifyAnchorableObjectRemoved(PhysicalHandsAnchorableBehaviour anchObj)
        {
            anchorableObjects.Remove(anchObj);
        }

    }

}