/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Interaction;
using UnityEngine;

namespace Leap.Unity.InteractionEngine.Examples
{

    [AddComponentMenu("")]
    public class SimpleMatchAnchorScaleAndState : MonoBehaviour
    {

        public AnchorableBehaviour anchObj;

        void Update()
        {
            if (anchObj != null && anchObj.anchor != null && anchObj.isAttached)
            {
                anchObj.transform.localScale = anchObj.anchor.transform.localScale;

                anchObj.gameObject.SetActive(anchObj.anchor.gameObject.activeInHierarchy);

                if (!anchObj.gameObject.activeInHierarchy)
                {
                    anchObj.transform.position = anchObj.anchor.transform.position;
                    if (anchObj.anchorRotation)
                        anchObj.transform.rotation = anchObj.anchor.transform.rotation;
                }
            }
        }

    }

}