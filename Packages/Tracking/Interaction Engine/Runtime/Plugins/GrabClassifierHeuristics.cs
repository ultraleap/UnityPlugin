/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Interaction.Internal.InteractionEngineUtility
{
    public class GrabClassifierHeuristics
    {
        public static void UpdateClassifier(GrabClassifier classifier,
                                            ClassifierParameters grabParameters,
                                            ref Collider[][] collidingCandidates,
                                            ref int[] numberOfColliders,
                                            bool ignoreTemporal = false)
        {
            // Store actual minimum curl in case we override it with the ignoreTemporal flag.
            float tempMinCurl = grabParameters.MINIMUM_CURL;
            if (ignoreTemporal)
            {
                grabParameters.MINIMUM_CURL = -1f;
            }

            //For each probe (fingertip)
            for (int j = 0; j < classifier.probes.Length; j++)
            {
                //Calculate how extended the finger is
                float tempCurl = Vector3.Dot(classifier.probes[j].direction, (j != 0) ? classifier.handDirection : (classifier.handChirality ? 1f : -1f) * classifier.handXBasis);
                float curlVelocity = tempCurl - classifier.probes[j].prevTempCurl;
                classifier.probes[j].prevTempCurl = tempCurl;

                //Determine if this probe is intersecting an object
                bool collidingWithObject = false;
                for (int i = 0; i < numberOfColliders[j]; i++)
                {
                    if (collidingCandidates[j][i].attachedRigidbody != null && collidingCandidates[j][i].attachedRigidbody == classifier.body)
                    {
                        collidingWithObject = true;
                        break;
                    }
                }

                //Nullify above findings if fingers are extended
                float conditionalMaxCurlVelocity = (classifier.isGrabbed ?
                                                    grabParameters.GRABBED_MAXIMUM_CURL_VELOCITY :
                                                    grabParameters.MAXIMUM_CURL_VELOCITY);
                collidingWithObject = collidingWithObject
                                      && (tempCurl < grabParameters.MAXIMUM_CURL)
                                      && (tempCurl > grabParameters.MINIMUM_CURL)
                                      && (ignoreTemporal
                                          || curlVelocity < conditionalMaxCurlVelocity);

                //Probes go inside when they intersect, probes come out when they uncurl
                if (!classifier.probes[j].isInside)
                {
                    classifier.probes[j].isInside = collidingWithObject;
                    classifier.probes[j].curl = tempCurl + (j == 0 ? grabParameters.THUMB_STICKINESS : grabParameters.FINGER_STICKINESS);
                    if (ignoreTemporal)
                    {
                        classifier.probes[j].curl = 0f + (j == 0 ? grabParameters.THUMB_STICKINESS : grabParameters.FINGER_STICKINESS);
                    }
                }
                else
                {
                    if (tempCurl > classifier.probes[j].curl)
                    {
                        classifier.probes[j].isInside = collidingWithObject;
                    }
                }
            }

            //If thumb and one other finger is "inside" the object, it's a grab!
            //This is the trick!
            classifier.isThisControllerGrabbing = (classifier.probes[0].isInside && (classifier.probes[1].isInside ||
                                                                       classifier.probes[2].isInside ||
                                                                       classifier.probes[3].isInside ||
                                                                       classifier.probes[4].isInside));
            //If grabbing within 10 frames of releasing, discard grab.
            //Suppresses spurious regrabs and makes throws work better.
            if (classifier.coolDownProgress <= grabParameters.GRAB_COOLDOWN && !ignoreTemporal)
            {
                if (classifier.isThisControllerGrabbing)
                {
                    classifier.isThisControllerGrabbing = false;
                }
                classifier.coolDownProgress += Time.fixedDeltaTime;
            }

            //Determine if the object is near the hand or if it's too far away
            if (classifier.isThisControllerGrabbing && !classifier.prevThisControllerGrabbing)
            {
                bool nearObject = false;
                numberOfColliders[5] = Physics.OverlapSphereNonAlloc(classifier.handGrabCenter, grabParameters.MAXIMUM_DISTANCE_FROM_HAND, collidingCandidates[5], grabParameters.LAYER_MASK, grabParameters.GRAB_TRIGGERS);
                for (int i = 0; i < numberOfColliders[5]; i++)
                {
                    if (collidingCandidates[5][i].attachedRigidbody != null && collidingCandidates[5][i].attachedRigidbody == classifier.body)
                    {
                        nearObject = true;
                        break;
                    }
                }

                if (!nearObject)
                {
                    classifier.isThisControllerGrabbing = false;
                    classifier.probes[0].isInside = false;
                }
            }

            // Reset the minimum curl parameter if we modified it due to the ignoreTemporal
            // flag.
            if (ignoreTemporal)
            {
                grabParameters.MINIMUM_CURL = tempMinCurl;
            }
        }

        //Expensive collider query optimization that somehow got undone before
        public static void UpdateAllProbeColliders(Vector3[] aPositions, Vector3[] bPositions, ref Collider[][] collidingCandidates, ref int[] numberOfColliders, ClassifierParameters grabParameters)
        {
            for (int i = 0; i < 5; i++)
            {
                numberOfColliders[i] = Physics.OverlapCapsuleNonAlloc(
                                         point0: aPositions[i],
                                         point1: bPositions[i],
                                         radius: i == 0 ? grabParameters.THUMBTIP_RADIUS : grabParameters.FINGERTIP_RADIUS,
                                         results: collidingCandidates[i],
                                         layerMask: grabParameters.LAYER_MASK,
                                         queryTriggerInteraction: grabParameters.GRAB_TRIGGERS);
            }
        }

        //Per-Object Per-Hand Classifier
        public class GrabClassifier
        {
            public bool isThisControllerGrabbing;
            public bool prevThisControllerGrabbing;
            public GrabProbe[] probes = new GrabProbe[5];
            public Transform transform;
            public Rigidbody body;
            public bool isGrabbed;
            public float coolDownProgress;
            public Vector3 handGrabCenter;
            public Vector3 handDirection;
            public Vector3 handXBasis;
            public bool handChirality;

            public GrabClassifier(GameObject behaviour)
            {
                probes = new GrabProbe[5];
                for (int i = 0; i < probes.Length; i++) { probes[i].prevTempCurl = 1f; }
                transform = behaviour.transform;
                body = behaviour.GetComponent<Rigidbody>();
                coolDownProgress = 0;
            }
        }

        //Per-Finger Per-Object Probe
        public struct GrabProbe
        {
            public Vector3 direction;
            public bool isInside;
            public float curl;
            public float prevTempCurl;
        };

        //The parameters that tune how grabbing feels
        public struct ClassifierParameters
        {
            /** <summary> The amount of curl hysteresis on each finger type </summary> */
            public float FINGER_STICKINESS, THUMB_STICKINESS;
            /** <summary> The minimum and maximum curl values fingers are allowed to "Grab" within </summary> */
            public float MAXIMUM_CURL, MINIMUM_CURL;
            /** <summary> The radius considered for intersection around the fingertips </summary> */
            public float FINGERTIP_RADIUS, THUMBTIP_RADIUS;
            /** <summary> The minimum amount of time between repeated grabs of a single object </summary> */
            public float GRAB_COOLDOWN;
            /** <summary> The maximum rate that the fingers are extending where grabs are considered. </summary> */
            public float MAXIMUM_CURL_VELOCITY;
            public float GRABBED_MAXIMUM_CURL_VELOCITY;
            public float MAXIMUM_DISTANCE_FROM_HAND;
            public int LAYER_MASK;
            public QueryTriggerInteraction GRAB_TRIGGERS;

            public ClassifierParameters(float fingerStickiness = 0f, float thumbStickiness = 0.05f, float maxCurl = 0.65f, float minCurl = -0.1f, float fingerRadius = 0.01f, float thumbRadius = 0.015f, float grabCooldown = 0.2f, float maxCurlVel = 0.0f, float grabbedMaxCurlVel = -0.05f, float maxHandDist = 0.1f, int layerMask = 0, QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.UseGlobal)
            {
                FINGER_STICKINESS = fingerStickiness;
                THUMB_STICKINESS = thumbStickiness;
                MAXIMUM_CURL = maxCurl;
                MINIMUM_CURL = minCurl;
                FINGERTIP_RADIUS = fingerRadius;
                THUMBTIP_RADIUS = thumbRadius;
                GRAB_COOLDOWN = grabCooldown;
                MAXIMUM_CURL_VELOCITY = maxCurlVel;
                GRABBED_MAXIMUM_CURL_VELOCITY = grabbedMaxCurlVel;
                MAXIMUM_DISTANCE_FROM_HAND = maxHandDist;
                LAYER_MASK = layerMask;
                GRAB_TRIGGERS = queryTriggers;
            }
        }
    }
}