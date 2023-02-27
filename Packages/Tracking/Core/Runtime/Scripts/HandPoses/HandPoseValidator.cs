using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandPoseValidator : MonoBehaviour
{

    /// <summary>
    /// Which hand would you like to use for gesture recognition?
    /// If this is left blank, It will search for all hands in the scene
    /// </summary>
    List<CapsuleHand> angleVisualisationHands = new List<CapsuleHand>();

    private Color[] capsuleHandColours = null;


    public void ShowJointColour(int fingerNum, int jointNum, float boneDifferenceToThreshold, float jointRotationThreshold)
    {
        if (angleVisualisationHands.Count <= 0)
        {
            angleVisualisationHands = GameObject.FindObjectsOfType<CapsuleHand>().ToList();
            if (angleVisualisationHands.Count <= 0)
            {
                Debug.Log("Skipping pose detection, there are no Leap hands in the scene");
                return;
            }
        }

        var colourCapsuleHand = angleVisualisationHands.FirstOrDefault();
        if (colourCapsuleHand != null)
        {
            if (capsuleHandColours == null)
            {
                capsuleHandColours = colourCapsuleHand.SphereColors;
            }
        }
        if (capsuleHandColours != null)
        {
            capsuleHandColours[(fingerNum * 4) + jointNum] = Color.Lerp(Color.green, Color.red, boneDifferenceToThreshold / 180);
        }


        foreach (var item in angleVisualisationHands)
        {
            var capsuleHand = (CapsuleHand)item;
            if (capsuleHand != null && capsuleHand != null)
            {
                capsuleHand.SetIndividualSphereColors = true;
                capsuleHand.SphereColors = capsuleHandColours;
            }
        }
    }

}
