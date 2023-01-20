/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Preview.Locomotion;
using UnityEngine;

namespace Leap.Unity.Examples.Preview
{
    /// <summary>
    /// Alternates between the active Teleportation techniques depending on which was last teleported to
    /// </summary>
    public class TeleportExampleManager : MonoBehaviour
    {
        public TeleportAnchor pinchToTeleportAnchor, jumpGemTeleportAnchor;
        public GameObject pinchToTeleport;
        public GameObject jumpGemTeleport;

        private TeleportActionBase _pinchToTeleportAction, _jumpGemTeleportAction;

        private void Start()
        {
            EnableTeleportAction(pinchToTeleport);
            pinchToTeleport.GetComponentInChildren<TeleportActionBase>().TeleportToAnchor(pinchToTeleportAnchor);

            _pinchToTeleportAction = pinchToTeleport.GetComponentInChildren<TeleportActionBase>();
            _jumpGemTeleportAction = jumpGemTeleport.GetComponentInChildren<TeleportActionBase>();

            _pinchToTeleportAction.RemoveTeleportAnchorFromFixedAnchors(_jumpGemTeleportAction.freeTeleportAnchor);

            _jumpGemTeleportAction.RemoveTeleportAnchorFromFixedAnchors(_pinchToTeleportAction.freeTeleportAnchor);
        }

        private void OnEnable()
        {
            pinchToTeleportAnchor.OnTeleportedTo += OnTeleportedToAnchor;
            jumpGemTeleportAnchor.OnTeleportedTo += OnTeleportedToAnchor;
        }

        private void OnDisable()
        {
            pinchToTeleportAnchor.OnTeleportedTo -= OnTeleportedToAnchor;
            jumpGemTeleportAnchor.OnTeleportedTo -= OnTeleportedToAnchor;
        }

        private void OnTeleportedToAnchor(TeleportAnchor anchor)
        {
            pinchToTeleport.GetComponentInChildren<TeleportActionBase>().SetLastTeleportedAnchor(anchor);
            jumpGemTeleport.GetComponentInChildren<TeleportActionBase>().SetLastTeleportedAnchor(anchor);

            if (anchor == pinchToTeleportAnchor)
            {
                EnableTeleportAction(pinchToTeleport);
            }
            else if (anchor == jumpGemTeleportAnchor)
            {
                EnableTeleportAction(jumpGemTeleport);
            }
        }

        private void EnableTeleportAction(GameObject teleportAction)
        {
            pinchToTeleport.SetActive(false);
            jumpGemTeleport.SetActive(false);

            teleportAction.SetActive(true);
        }
    }
}