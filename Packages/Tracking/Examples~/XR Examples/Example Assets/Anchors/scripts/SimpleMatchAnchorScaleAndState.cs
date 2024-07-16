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
    public class SimpleMatchAnchorScaleAndState : MonoBehaviour
    {
        public AnchorableBehaviour anchObj;

        void Update()
        {
            if (anchObj != null && anchObj.Anchor != null && anchObj.isAttached)
            {
                anchObj.transform.localScale = anchObj.Anchor.transform.localScale;

                anchObj.gameObject.SetActive(anchObj.Anchor.gameObject.activeInHierarchy);

                if (!anchObj.gameObject.activeInHierarchy)
                {
                    anchObj.transform.position = anchObj.Anchor.transform.position;
                    if (anchObj.anchorRotation)
                        anchObj.transform.rotation = anchObj.Anchor.transform.rotation;
                }
            }
        }
    }
}