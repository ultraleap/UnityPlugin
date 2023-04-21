/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
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
        [SerializeField]
        private HandModelManager handModelManager;
        private int currentHandID;

        // Use this for initialization
        void Start()
        {
            if (handModelManager == null)
            {
                handModelManager = FindObjectOfType<HandModelManager>();
                if (handModelManager == null)
                {
                    Debug.LogWarning("CycleHandPairs needs a HandModelManager in the scene");
                    return;
                }
            }
            currentHandID = 0;
            handModelManager.EnableHandModelPair(currentHandID, disableOtherHandModels: true);
        }

        // Update is called once per frame
        void Update()
        {
            if (handModelManager == null)
            {
                return;
            }

            if (Input.GetKeyUp(KeyCode.RightArrow))
            {
                NextHandSet();
            }

            if (Input.GetKeyUp(KeyCode.LeftArrow))
            {
                PreviousHandSet();
            }
        }

        private void NextHandSet()
        {
            currentHandID++;
            if (currentHandID > handModelManager.HandModelPairs.Count - 1) currentHandID = 0;
            handModelManager.EnableHandModelPair(currentHandID, disableOtherHandModels: true);
        }

        private void PreviousHandSet()
        {
            currentHandID--;
            if (currentHandID < 0) currentHandID = handModelManager.HandModelPairs.Count - 1;
            handModelManager.EnableHandModelPair(currentHandID, disableOtherHandModels: true);
        }
    }
}