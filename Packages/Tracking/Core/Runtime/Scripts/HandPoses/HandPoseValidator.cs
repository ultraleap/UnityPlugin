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
    public List<CapsuleHand> angleVisualisationHands = new List<CapsuleHand>();

    private Color[] _capsuleHandColours = new Color[32];
    private HandPoseDetector _detector;

    private void Start()
    {
        _detector = FindObjectOfType<HandPoseDetector>();
    }

    private void Update()
    {
        if (_detector != null) 
        {
            angleVisualisationHands = GameObject.FindObjectsOfType<CapsuleHand>().ToList();

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
                Debug.Log("Skipping pose detection, there are no Leap hands in the scene");
                return;
            }
        }
    }
}
