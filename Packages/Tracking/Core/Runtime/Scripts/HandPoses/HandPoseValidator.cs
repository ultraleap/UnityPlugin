using Leap.Unity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandPoseValidator : MonoBehaviour
{

    /// <summary>
    /// Which hand would you like to use for gesture recognition?
    /// If this is left blank, It will search for all hands in the scene
    /// </summary>
    public List<CapsuleHand> angleVisualisationHands = new List<CapsuleHand>();

    private Color[] capsuleHandColours = null;
    private HandPoseDetector detector;

    private void Start()
    {
        detector = FindObjectOfType<HandPoseDetector>();


    }

    private void Update()
    {
        if (detector != null) 
        {
            angleVisualisationHands = GameObject.FindObjectsOfType<CapsuleHand>().ToList();

            var colourCapsuleHand = angleVisualisationHands.FirstOrDefault();
            if (colourCapsuleHand != null)
            {
                if (capsuleHandColours == null)
                {
                    capsuleHandColours = colourCapsuleHand.SphereColors;
                }
            }

            if (angleVisualisationHands.Count > 0)
            {
                var validationData = detector.GetValidationData();

                foreach (var visHand in angleVisualisationHands)
                {
                    foreach (var data in validationData)
                    {
                        if (data.chirality == visHand.Handedness)
                        {
                            if(data.withinThreshold)
                            {
                                capsuleHandColours[(data.fingerNum * 4) + data.jointNum] = Color.green;
                            }
                            else
                            {
                                capsuleHandColours[(data.fingerNum * 4) + data.jointNum] = Color.red;
                            }
                        }
                    }
                    if (visHand != null)
                    {
                        visHand.SetIndividualSphereColors = true;
                        visHand.SphereColors = capsuleHandColours;
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
