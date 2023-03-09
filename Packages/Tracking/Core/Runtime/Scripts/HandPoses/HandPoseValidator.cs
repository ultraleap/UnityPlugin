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
    private List<CapsuleHand> angleVisualisationHands = new List<CapsuleHand>();

    private Color[] _capsuleHandColours = new Color[32];
    private HandPoseDetector _detector;

    private void Start()
    {
        _detector = FindObjectOfType<HandPoseDetector>();
        
    }
    List<GameObject> lineRenderers = new List<GameObject>();

    private void Update()
    {
        if(angleVisualisationHands.Count != GameObject.FindObjectsOfType<CapsuleHand>().ToList().Count)
        {
            foreach (var lineRenderer in lineRenderers)
            {
                DestroyImmediate(lineRenderer);
            }
            lineRenderers.Clear();
        }
        angleVisualisationHands = GameObject.FindObjectsOfType<CapsuleHand>().ToList();
        if (_detector != null) 
        {
            var colourCapsuleHand = angleVisualisationHands.FirstOrDefault();
            if (colourCapsuleHand != null)
            {
                Utils.Fill(_capsuleHandColours, Color.grey);
            }


            if (angleVisualisationHands.Count > 0)
            {
                var validationData = _detector.GetValidationData();

                foreach (var visHand in angleVisualisationHands)
                {
                    foreach (var data in validationData)
                    {
                        if (data.chirality == visHand.Handedness)
                        {
                            if(data.withinThreshold)
                            {
                                _capsuleHandColours[(data.fingerNum * 4) + data.jointNum] = Color.green;
                            }
                            else
                            {
                                _capsuleHandColours[(data.fingerNum * 4) + data.jointNum] = Color.red;
                            }
                        }
                    }
                    if (visHand != null)
                    {
                        visHand.SetIndividualSphereColors = true;
                        visHand.SphereColors = _capsuleHandColours;
                    }
                }
            }
            else
            {

                return;
            }

            var lineRenderCount = 0;
            for (int j = 0; j < angleVisualisationHands.Count; j++)
            {
                for (int i = 0; i < _detector.BoneDirectionTargets.Count; i++)
                {
                    var boneDirectionTarget = _detector.BoneDirectionTargets.ElementAt(i);

                    if (boneDirectionTarget.enabled)
                    {

                        var capsuleHand = angleVisualisationHands.ElementAt(j);

                        if (lineRenderers.ElementAtOrDefault(lineRenderCount) == null)
                        {
                            var lineRendChild = new GameObject();
                            lineRendChild.transform.SetParent(capsuleHand.gameObject.transform);
                            lineRendChild.AddComponent<LineRenderer>();
                            lineRenderers.Add(lineRendChild);
                        }


                        if (lineRenderers.ElementAt(i).GetComponent<LineRenderer>())
                        {
                            var lineRend = lineRenderers.ElementAt(lineRenderCount).GetComponent<LineRenderer>();
                            lineRend.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
                            lineRend.startWidth = 0.01f;
                            lineRend.endWidth = 0.01f;
                            lineRend.material.color = new Color(0, 235, 133, 0.7f);

                            if (capsuleHand != null && capsuleHand.enabled)
                            {
                                if (!boneDirectionTarget.isPalmDirection &&
                                    boneDirectionTarget.fingerTypeForPoint != Leap.Finger.FingerType.TYPE_UNKNOWN &&
                                    boneDirectionTarget.boneForPoint != Leap.Bone.BoneType.TYPE_INVALID)
                                {
                                    int fingNum = (int)boneDirectionTarget.fingerTypeForPoint;
                                    int boneNum = (int)boneDirectionTarget.boneForPoint;
                                    if (capsuleHand.GetLeapHand() != null)
                                    {
                                        var directionBone = capsuleHand.GetLeapHand().Fingers[fingNum].bones[boneNum];
                                        if (directionBone.PrevJoint != null)
                                        {
                                            Ray ray = new Ray(directionBone.PrevJoint, directionBone.Direction);

                                            lineRend.SetPosition(0, directionBone.PrevJoint);
                                            lineRend.SetPosition(1, ray.GetPoint(100));
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
                                        lineRend.SetPosition(1, ray.GetPoint(100));
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
