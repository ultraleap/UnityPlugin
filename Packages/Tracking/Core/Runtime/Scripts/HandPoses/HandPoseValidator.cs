using Leap.Unity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandPoseValidator : MonoBehaviour
{
    /// <summary>
    /// Which hand would you like to use for gesture validation?
    /// If this is left blank, It will search for all hands in the scene
    /// </summary>
    [SerializeField]
    private List<CapsuleHand> validationHands = new List<CapsuleHand>();
    private List<CapsuleHand> storedValidationHands = new List<CapsuleHand>();
    private int validationHandsActive = 0;
    private int validationHandsActivePrevFrame = 0;

    [SerializeField]
    private HandPoseDetector poseDetector;

    [SerializeField]
    private LeapProvider leapProvider = null;

    private Color[] leftCapsuleHandColours = new Color[32];
    private Color[] rightCapsuleHandColours = new Color[32];

    public Transform validatorHandPrefab;

    List<GameObject> lineRenderers = new List<GameObject>();

    private void Start()
    {
        if (poseDetector == null)
        {
            poseDetector = FindObjectOfType<HandPoseDetector>();
        }

        poseDetector.EnablePoseCaching();

        if (validationHands.Count == 0)
        {
            Transform instCapsuleHands = Instantiate(validatorHandPrefab);
            CapsuleHand[] capsuleHandScript = instCapsuleHands.GetComponentsInChildren<CapsuleHand>(true);

            foreach (var capHand in capsuleHandScript)
            {
                capHand.leapProvider = leapProvider;
                storedValidationHands.Add(capHand);
                validationHandsActive++;
            }
        }
        else
        {
            storedValidationHands = validationHands;
        }
    }

    private void Update()
    {
        foreach (var hand in storedValidationHands)
        {
            if (!hand.isActiveAndEnabled)
            {
                validationHandsActive--;
            }
        }

        if (validationHandsActive != validationHandsActivePrevFrame)
        {
            validationHandsActivePrevFrame = validationHandsActive;

            foreach (var lineRenderer in lineRenderers)
            {
                DestroyImmediate(lineRenderer);
            }

            lineRenderers.Clear();
        }

        if (poseDetector != null)
        {
            ColorHandJoints();
            RenderDirectionRays();
        }
    }

    private void ColorHandJoints()
    {
        CapsuleHand colourCapsuleHand = storedValidationHands.FirstOrDefault();

        if (colourCapsuleHand != null)
        {
            Leap.Unity.Utils.Fill(leftCapsuleHandColours, Color.grey);
            Leap.Unity.Utils.Fill(rightCapsuleHandColours, Color.grey);
        }

        if (storedValidationHands.Count > 0)
        {
            List<HandPoseDetector.ValidationData> validationData = poseDetector.GetValidationData();

            foreach (var visHand in storedValidationHands)
            {
                foreach (var data in validationData)
                {
                    if (data.withinThreshold)
                    {
                        if (visHand.Handedness == Chirality.Left && data.chirality == Chirality.Left)
                        {
                            leftCapsuleHandColours[(data.fingerNum * 4) + data.boneNum] = Color.green;
                        }
                        else if (visHand.Handedness == Chirality.Right && data.chirality == Chirality.Right)
                        {
                            rightCapsuleHandColours[(data.fingerNum * 4) + data.boneNum] = Color.green;
                        }
                    }
                    else
                    {
                        if (visHand.Handedness == Chirality.Left && data.chirality == Chirality.Left)
                        {
                            leftCapsuleHandColours[(data.fingerNum * 4) + data.boneNum] = Color.red;
                        }
                        else if (visHand.Handedness == Chirality.Right && data.chirality == Chirality.Right)
                        {
                            rightCapsuleHandColours[(data.fingerNum * 4) + data.boneNum] = Color.red;
                        }
                    }
                }

                if (visHand != null)
                {
                    visHand.SetIndividualSphereColors = true;

                    if (visHand.Handedness == Chirality.Left)
                    {
                        visHand.SphereColors = leftCapsuleHandColours;
                    }
                    else
                    {
                        visHand.SphereColors = rightCapsuleHandColours;
                    }
                }
            }
        }
        else
        {
            return;
        }
    }

    private void RenderDirectionRays()
    {
        int lineRenderCount = 0;

        for (int j = 0; j < storedValidationHands.Count; j++)
        {
            if (storedValidationHands.ElementAt(j).enabled)
            {
                for (int i = 0; i < poseDetector.poseRules.Count; i++)
                {
                    var boneDirectionTarget = poseDetector.poseRules.ElementAt(i);

                    if (boneDirectionTarget.enabled)
                    {
                        bool AtleastOneDirectionActive = false;

                        foreach (var direction in boneDirectionTarget.directions)
                        {
                            if (direction.enabled)
                            {
                                AtleastOneDirectionActive = true;
                            }
                        }

                        if (AtleastOneDirectionActive)
                        {
                            Color lineColor = Color.gray;
                            CapsuleHand capsuleHand = storedValidationHands.ElementAt(j);
                            foreach (var directionForValidator in poseDetector.poseRulesForValidator)
                            {
                                if (directionForValidator.chirality == capsuleHand.Handedness &&
                                    directionForValidator.poseRuleAndStatus.Item1.finger == boneDirectionTarget.finger)
                                {
                                    if (directionForValidator.poseRuleAndStatus.Item2)
                                    {
                                        lineColor = Color.green;
                                    }
                                    else
                                    {
                                        lineColor = Color.red;
                                    }
                                }
                            }

                            if (lineRenderers.ElementAtOrDefault(lineRenderCount) == null)
                            {
                                var lineRendChild = new GameObject();
                                lineRendChild.transform.SetParent(capsuleHand.gameObject.transform);
                                lineRendChild.AddComponent<LineRenderer>();
                                lineRenderers.Add(lineRendChild);
                            }

                            var lineRend = lineRenderers.ElementAt(lineRenderCount).GetComponent<LineRenderer>();

                            if (lineRend)
                            {
                                lineRend.material = new Material(Shader.Find("Particles/Standard Unlit"));
                                lineRend.startWidth = 0.005f;
                                lineRend.endWidth = 0.005f;
                                lineRend.material.color = lineColor;

                                if (capsuleHand != null && capsuleHand.enabled)
                                {
                                    if ((int)boneDirectionTarget.finger != 5 &&
                                        boneDirectionTarget.finger != (int)Leap.Finger.FingerType.TYPE_UNKNOWN &&
                                        boneDirectionTarget.bone != (int)Leap.Bone.BoneType.TYPE_INVALID)
                                    {
                                        int fingNum = (int)boneDirectionTarget.finger;
                                        int boneNum = (int)boneDirectionTarget.bone;

                                        if (capsuleHand.GetLeapHand() != null)
                                        {
                                            var directionBone = capsuleHand.GetLeapHand().Fingers[fingNum].bones[boneNum];

                                            if (directionBone.PrevJoint != null)
                                            {
                                                Ray ray = new Ray(directionBone.PrevJoint, directionBone.Direction);

                                                lineRend.SetPosition(0, directionBone.PrevJoint);
                                                lineRend.SetPosition(1, ray.GetPoint(0.1f));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (capsuleHand.GetLeapHand() != null &&
                                            capsuleHand.GetLeapHand().PalmPosition != null &&
                                            capsuleHand.GetLeapHand().PalmNormal != null)
                                        {
                                            Ray ray = new Ray(capsuleHand.GetLeapHand().PalmPosition, capsuleHand.GetLeapHand().PalmNormal);

                                            lineRend.SetPosition(0, capsuleHand.GetLeapHand().PalmPosition);
                                            lineRend.SetPosition(1, ray.GetPoint(0.1f));
                                        }
                                    }
                                }
                            }
                            lineRenderCount++;
                        }
                    }
                }
            }
        }
    }
}