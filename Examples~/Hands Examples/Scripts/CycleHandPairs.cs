/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.HandsModule.Examples
{
    public class CycleHandPairs : MonoBehaviour
    {
        public List<GameObject> handList;
        private int currentHandID;

        // Use this for initialization
        void Start()
        {
            currentHandID = 0;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.RightArrow))
            {
                currentHandID++;
                if (currentHandID > handList.Count - 1) currentHandID = 0;
            }

            if (Input.GetKeyUp(KeyCode.LeftArrow))
            {
                currentHandID--;
                if (currentHandID < 0) currentHandID = handList.Count - 1;
            }

            SortHands();
        }

        void SortHands()
        {
            for (int i = 0; i < handList.Count; i++)
            {
                var hand = handList[i];
                hand.gameObject.SetActive(i == currentHandID ? true : false);
            }
        }
    }
}
