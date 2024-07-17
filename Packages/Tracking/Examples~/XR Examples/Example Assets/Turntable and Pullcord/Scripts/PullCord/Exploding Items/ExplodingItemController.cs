/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Examples
{
    /// <summary>
    /// Handles exploding object by being given a float value of progress and progresses all ExplodingItems that are the children of a given object
    /// </summary>
    public class ExplodingItemController : MonoBehaviour
    {
        [SerializeField]
        private Transform _explodingItemsRoot = null;

        private ExplodingItem[] _explodingItems;

        private void Start()
        {
            _explodingItems = _explodingItemsRoot.GetComponentsInChildren<ExplodingItem>(true);
        }

        /// <summary>
        /// Sets the percentage value of all child ExplodingItems of the _explodingItemsRoot with a given explosionProgress value
        /// </summary>
        public void UpdateItems(float explosionProgress)
        {
            foreach (ExplodingItem explodingItem in _explodingItems)
            {
                explodingItem.SetPercent(explosionProgress);
            }
        }
    }
}