/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Attachments
{

    public class AttachmentHandEnableDisable : MonoBehaviour
    {
        public AttachmentHand attachmentHand;

        void Update()
        {
            // Deactivation trigger
            if (!attachmentHand.isTracked && attachmentHand.gameObject.activeSelf)
            {
                attachmentHand.gameObject.SetActive(false);
            }

            // Reactivation trigger
            if (attachmentHand.isTracked && !attachmentHand.gameObject.activeSelf)
            {
                attachmentHand.gameObject.SetActive(true);
            }
        }
    }
}