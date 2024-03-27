/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Interaction;
using UnityEngine;

namespace Leap.InteractionEngine.Examples
{
    /// <summary>
    /// Displays a GrabBalls current state by representing restricted areas with a ghostedMesh and scaling the defaultMesh when close to interaction.
    /// </summary>
    public class SimpleGrabBallFeedback : MonoBehaviour
    {
        [Header("Setup")]
        public GrabBall grabBall;
        public MeshRenderer defaultMesh, ghostedMesh;

        [Header("Scaling")]
        [SerializeField, Tooltip("The maximum distance to the nearest interacting hand required before the GrabBall will scale")]
        private float distanceToScaleGrabBall = 0.1f;

        [SerializeField, Tooltip("The localscale value that the GrabBall will scale to when near an interacting hand")]
        private Vector3 expandedScale = new Vector3(1, 1, 1);

        [SerializeField, Tooltip("The localscale value that the GrabBall will scale to when idle or not near an interacting hand")]
        private Vector3 minimisedScale = new Vector3(0.5f, 0.5f, 0.5f);

        [SerializeField, Tooltip("A speed multiplier for scaling, the higher the lerpTime, the faster the scaling")]
        private float lerpTime = 6f;

        private void Start()
        {
            // Try to find an attached GrabBall, or warn that we require one
            if (grabBall == null)
            {
                grabBall = GetComponent<GrabBall>();

                if (grabBall == null)
                {
                    Debug.LogWarning("No grab ball assigned to GrabBallMeshVisuals", gameObject);
                }
            }
        }

        void Update()
        {
            if (grabBall == null)
            {
                return;
            }

            defaultMesh.transform.rotation = LookAtRotationParallelToHorizon(defaultMesh.transform.position, Camera.main.transform.position);

            bool expanded = grabBall.grabBallInteractionBehaviour.closestHoveringControllerDistance < distanceToScaleGrabBall;
            defaultMesh.transform.localScale = Vector3.Lerp(defaultMesh.transform.localScale, (expanded) ? expandedScale : minimisedScale, Time.deltaTime * lerpTime);

            if (grabBall.grabBallRestrictionStatus.IsRestricted && grabBall.grabBallInteractionBehaviour.isGrasped)
            {
                ghostedMesh.gameObject.SetActive(true);
                ghostedMesh.transform.position = grabBall.grabBallPose.position;
                ghostedMesh.transform.rotation = grabBall.grabBallPose.rotation;
            }
            else
            {
                ghostedMesh.gameObject.SetActive(false);
            }
        }

        private Quaternion LookAtRotationParallelToHorizon(Vector3 posA, Vector3 posB)
        {
            return Quaternion.AngleAxis(Quaternion.LookRotation((posA - posB).normalized).eulerAngles.y, Vector3.up);
        }
    }
}