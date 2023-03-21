using Leap.Unity;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
    private LeapProvider leapProvider = null;

    private Color[] _leftCapsuleHandColours = new Color[32];
    private Color[] _rightCapsuleHandColours = new Color[32];
    private HandPoseDetector _detector;

    List<GameObject> lineRenderers = new List<GameObject>();


    private void Start()
    {
        _detector = FindObjectOfType<HandPoseDetector>();
        _detector.EnablePoseCaching();
        if(validationHands.Count == 0)
        {
            var instCapsuleHands = Instantiate(CreateCapsuleHands());
            var capsuleHandScript = instCapsuleHands.GetComponentsInChildren<CapsuleHand>(true);
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

    public static GameObject CreateCapsuleHands()
    {
        string prefabName = "CapsuleHands";
        var guids = AssetDatabase.FindAssets(prefabName);

        // look for exact matched first
        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            string[] assetPathSplit = assetPath.Split('/', '\\', '.');

            if (assetPathSplit[assetPathSplit.Length - 2] == prefabName && assetPathSplit[assetPathSplit.Length - 1] == "prefab")
            {
                GameObject newObject = (GameObject)AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));

                if (newObject != null)
                {
                    return newObject;
                }
            }
        }

        // fallback to near-matches
        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject newObject = (GameObject)AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));

            if (newObject != null)
            {
                return newObject;
            }
        }
        return null;
    }


    private void Update()
    {
        foreach (var hand in storedValidationHands)
        {
            if(!hand.isActiveAndEnabled)
            {
                validationHandsActive--;
            }
        }

        if(validationHandsActive != validationHandsActivePrevFrame)
        {
            validationHandsActivePrevFrame = validationHandsActive;

            foreach (var lineRenderer in lineRenderers)
            {
                DestroyImmediate(lineRenderer);
            }

            lineRenderers.Clear();
        }

        if (_detector != null) 
        {
            ColorHandJoints();
            RenderDirectionRays();
        }
    }

    private void ColorHandJoints()
    {
        var colourCapsuleHand = storedValidationHands.FirstOrDefault();

        if (colourCapsuleHand != null)
        {
            Utils.Fill(_leftCapsuleHandColours, Color.grey);
            Utils.Fill(_rightCapsuleHandColours, Color.grey);
        }

        if (storedValidationHands.Count > 0)
        {
            var validationData = _detector.GetValidationData();

            foreach (var visHand in storedValidationHands)
            {
                foreach (var data in validationData)
                {
                    if (data.withinThreshold)
                    {
                        if (visHand.Handedness == Chirality.Left && data.chirality == Chirality.Left)
                        {
                            _leftCapsuleHandColours[(data.fingerNum * 4) + data.jointNum] = Color.green;
                        }
                        else if (visHand.Handedness == Chirality.Right && data.chirality == Chirality.Right)
                        {
                            _rightCapsuleHandColours[(data.fingerNum * 4) + data.jointNum] = Color.green;
                        }
                    }
                    else
                    {
                        if (visHand.Handedness == Chirality.Left && data.chirality == Chirality.Left)
                        {
                            _leftCapsuleHandColours[(data.fingerNum * 4) + data.jointNum] = Color.red;
                        }
                        else if (visHand.Handedness == Chirality.Right && data.chirality == Chirality.Right)
                        {
                            _rightCapsuleHandColours[(data.fingerNum * 4) + data.jointNum] = Color.red;
                        }
                    }
                }

                if (visHand != null)
                {
                    visHand.SetIndividualSphereColors = true;

                    if (visHand.Handedness == Chirality.Left)
                    {
                        visHand.SphereColors = _leftCapsuleHandColours;
                    }
                    else
                    {
                        visHand.SphereColors = _rightCapsuleHandColours;
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
        var lineRenderCount = 0;
        for (int j = 0; j < storedValidationHands.Count; j++)
        {
            if (storedValidationHands.ElementAt(j).enabled)
            {
                for (int i = 0; i < _detector.Sources.Count; i++)
                {
                    var boneDirectionTarget = _detector.Sources.ElementAt(i);

                    if (boneDirectionTarget.enabled)
                    {
                        bool AtleastOneDirectionActive = false;

                        foreach (var direction in boneDirectionTarget.direction)
                        {
                            if (direction.enabled)
                            {
                                AtleastOneDirectionActive = true;
                            }
                        }

                        if (AtleastOneDirectionActive)
                        {
                            Color lineColor = Color.gray;
                            var capsuleHand = storedValidationHands.ElementAt(j);
                            foreach (var directionForValidator in _detector.poseDirectionsForValidator)
                            {
                                if (directionForValidator.chirality == capsuleHand.Handedness &&
                                    directionForValidator.sourceDirectionAndStatus.Item1.finger == boneDirectionTarget.finger)
                                {
                                    if (directionForValidator.sourceDirectionAndStatus.Item2)
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