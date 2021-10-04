/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Hands.Examples
{
    public class CycleHandPairs : MonoBehaviour
    {
        public List<GameObject> hands;
        private int currentGroup;

        // Use this for initialization
        void Start()
        {
            currentGroup = 0;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.RightArrow))
            {
                currentGroup++;
                if (currentGroup < 0) currentGroup = hands.Count - 1;
            }

            if (Input.GetKeyUp(KeyCode.LeftArrow))
            {
                currentGroup--;
                if (currentGroup > hands.Count - 1) currentGroup = 0;
            }

            SortHands();
        }

        void SortHands()
        {
            for (int i = 0; i < hands.Count; i++)
            {
                var hand = hands[i];
                hand.gameObject.SetActive(i == currentGroup ? true : false);
            }
        }
    }
}