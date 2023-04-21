/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity
{
    /**
     * A HandModel that draws lines for the bones in the hand and its fingers.
     *
     * The debugs lines are only drawn in the Editor Scene view (when a hand is tracked) and
     * not in the Game view. Use debug hands when you aren't using visible hands in a scene
     * so that you can see where the hands are in the scene view.
     * */
    public class DebugHand : HandModelBase
    {
        private Hand hand_;

        [SerializeField]
        private bool visualizeBasis = true;
        public bool VisualizeBasis { get { return visualizeBasis; } set { visualizeBasis = value; } }

        /** The colors used for each bone. */
        protected Color[] colors = { Color.gray, Color.yellow, Color.cyan, Color.magenta };

        public override ModelType HandModelType
        {
            get
            {
                return ModelType.Graphics;
            }
        }

        [SerializeField]
        private Chirality handedness;
        public override Chirality Handedness
        {
            get
            {
                return handedness;
            }
            set { }
        }

        public override Hand GetLeapHand()
        {
            return hand_;
        }

        public override void SetLeapHand(Hand hand)
        {
            hand_ = hand;
        }

        public override bool SupportsEditorPersistence()
        {
            return true;
        }

        /**
        * Initializes the hand and calls the line drawing function.
        */
        public override void InitHand()
        {
            DrawDebugLines();
        }

        /**
        * Updates the hand and calls the line drawing function.
        */
        public override void UpdateHand()
        {
            DrawDebugLines();
        }

        /**
        * Draws lines from elbow to wrist, wrist to palm, and normal to the palm.
        */
        protected void DrawDebugLines()
        {
            Hand hand = GetLeapHand();
            Debug.DrawLine(hand.Arm.ElbowPosition, hand.Arm.WristPosition, Color.red); //Arm
            Debug.DrawLine(hand.WristPosition, hand.PalmPosition, Color.white); //Wrist to palm line
            Debug.DrawLine(hand.PalmPosition, hand.PalmPosition + hand.PalmNormal * hand.PalmWidth / 2, Color.black); //Hand Normal

            if (VisualizeBasis)
            {
                DrawBasis(hand.PalmPosition, hand.Basis, hand.PalmWidth / 4); //Hand basis
                DrawBasis(hand.Arm.ElbowPosition, hand.Arm.Basis, .01f); //Arm basis
            }

            for (int f = 0; f < 5; f++)
            { //Fingers
                Finger finger = hand.Fingers[f];
                for (int i = 0; i < 4; ++i)
                {
                    Bone bone = finger.Bone((Bone.BoneType)i);
                    Debug.DrawLine(bone.PrevJoint, bone.PrevJoint + bone.Direction * bone.Length, colors[i]);
                    if (VisualizeBasis)
                        DrawBasis(bone.PrevJoint, bone.Basis, .01f);
                }
            }
        }

        public void DrawBasis(Vector3 position, LeapTransform basis, float scale)
        {
            Vector3 origin = position;
            Debug.DrawLine(origin, origin + basis.xBasis * scale, Color.red);
            Debug.DrawLine(origin, origin + basis.yBasis * scale, Color.green);
            Debug.DrawLine(origin, origin + basis.zBasis * scale, Color.blue);
        }

    }
}