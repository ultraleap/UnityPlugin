using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap;

public class PersistentHandsProvider : PostProcessProvider
{
    Hand lastLeft = null;
    Hand lastRight = null;

    public override void ProcessFrame(ref Frame inputFrame)
    {
        // Clone the hands so we don't pass a reference to ensure we keep 'lastHands' persistent 
        List<Hand> newHands = CloneHands(lastLeft, lastRight);

        // There are no input hands, populate them with whatever we are aware of
        if (inputFrame.Hands == null || inputFrame.Hands.Count == 0) {
            if (newHands != null && newHands.Count > 0) {
                inputFrame.Hands = newHands;
            }
            return;
        }

        /// Past this point we need to compare individual hands

        // newHands
        Hand newLeft = null;
        Hand newRight = null;

        // Find any new inputFrame hands
        foreach (var inputHand in inputFrame.Hands) {
            if(inputHand.IsLeft) {
                newLeft = inputHand;
            }

            if(inputHand.IsRight) {
                newRight = inputHand;
            }
        }

        CompareAndPopulate(ref newLeft, ref lastLeft);
        CompareAndPopulate(ref newRight, ref lastRight);

        // Clone the hands so we don't pass a reference to ensure we keep 'lastHands' persistent 
        newHands = CloneHands(newLeft, newRight);
        inputFrame.Hands = newHands;
    }

    /// <summary>
    /// Clone a pair of hands so that we store the data rather than an editable reference.
    /// </summary>
    /// <param name="_leftHand"></param>
    /// <param name="_rightHand"></param>
    /// <returns></returns>
    List<Hand> CloneHands(Hand _leftHand, Hand _rightHand)
    {
        List<Hand> newHands = new List<Hand>();

        if (_leftHand != null && _leftHand.Id != 0) {
            newHands.Add(CloneHand(_leftHand));
        }

        if (_rightHand != null && _rightHand.Id != 0) {
            newHands.Add(CloneHand(_rightHand));
        }

        return newHands;
    }

    /// <summary>
    /// Clone a hand so that we store the data rather than an editable reference.
    /// </summary>
    /// <param name="_handToClone"></param>
    /// <returns></returns>
    Hand CloneHand(Hand _handToClone)
    {
        Hand newHand = new Hand();
        newHand.CopyFrom(_handToClone);
        return newHand;
    }

    /// <summary>
    /// If we have new input hands, we should store them as 'lastHands' for future reference, 
    /// if not, we should use any 'lastHands' we have.
    /// </summary>
    /// <param name="_newHand"></param>
    /// <param name="_lastHand"></param>
    void CompareAndPopulate(ref Hand _newHand, ref Hand _lastHand)
    {
        if(_newHand != null) {
            _lastHand = _newHand;
        }
        else if (_lastHand != null && _lastHand.Id != 0) {
            _newHand = _lastHand;
        }
    }
}