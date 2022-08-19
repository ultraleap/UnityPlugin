/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.FarFieldInteractions
{
    /// <summary>
    /// specifies a rotation deadzone
    /// </summary>
    public class RotationDeadzone : MonoBehaviour
    {
        private struct TimestampDeadzoneCentre
        {
            public float Timestamp;
            public float DeadzoneCentre;

            public TimestampDeadzoneCentre(float timestamp, float deadzoneCentre) : this()
            {
                Timestamp = timestamp;
                DeadzoneCentre = deadzoneCentre;
            }
        }

        public float DeadzoneSize = 25;
        [Space]
        public bool UseRecentring = true;
        [Range(0, 360)] public float RecentreTimer = 1.5f;
        [Range(0, 360)] public float RecentreDelta = 10;
        [Range(0.01f, 30)] public float RecentreLerpSpeed = 1;

        [HideInInspector] public float DeadzoneCentre;
        [HideInInspector] public float RecentredPositionDelta;

        private bool initialised;
        private bool recentredWhilstStill;
        private Queue<TimestampDeadzoneCentre> deadzoneCentreHistory;

        private void Start()
        {

        }

        public void UpdateDeadzone(float headYRotation)
        {
            //Calculate Deadzone
            if (!initialised)
            {
                DeadzoneCentre = headYRotation;
                RecentredPositionDelta = 0;
                deadzoneCentreHistory = new Queue<TimestampDeadzoneCentre>();
                initialised = true;
                recentredWhilstStill = false;
                return;
            }

            //Map angle from 0-360 to -180-180
            float delta = headYRotation + RecentredPositionDelta - DeadzoneCentre;
            delta = SimplifyAngle(delta);
            float signDelta = Mathf.Sign(delta);
            float absDelta = Mathf.Abs(delta);

            if (absDelta > DeadzoneSize)
            {
                if (recentredWhilstStill)
                {
                    deadzoneCentreHistory.Clear();
                    recentredWhilstStill = false;
                }
                DeadzoneCentre += (absDelta - DeadzoneSize) * signDelta;
                DeadzoneCentre %= 360;

                //Reduce recentring offset
                if (absDelta > DeadzoneSize + 0.1f)
                {
                    RecentredPositionDelta = Mathf.Lerp(RecentredPositionDelta, 0, Time.deltaTime * RecentreLerpSpeed);
                }
            }

            //Calculate if need to recentre
            if (UseRecentring)
            {
                RecentreNeckRotationDeadzone(headYRotation);
            }
        }

        private void RecentreNeckRotationDeadzone(float headYRotation)
        {
            deadzoneCentreHistory.Enqueue(new TimestampDeadzoneCentre(Time.time, DeadzoneCentre));
            if (Time.time - deadzoneCentreHistory.Peek().Timestamp < RecentreTimer)
            {
                return;
            }

            while (Time.time - deadzoneCentreHistory.Peek().Timestamp > RecentreTimer)
            {
                deadzoneCentreHistory.Dequeue();
            }

            float recentreDelta = deadzoneCentreHistory.Peek().DeadzoneCentre - DeadzoneCentre;
            recentreDelta = SimplifyAngle(recentreDelta);
            recentreDelta = Mathf.Abs(recentreDelta);

            if (recentreDelta <= RecentreDelta && !recentredWhilstStill)
            {
                recentredWhilstStill = true;
                RecentredPositionDelta = DeadzoneCentre - headYRotation;
                RecentredPositionDelta = SimplifyAngle(RecentredPositionDelta);
                deadzoneCentreHistory.Clear();
            }
        }

        //Translates our angle to be in the range -180 - +180
        private float SimplifyAngle(float angle)
        {
            angle = angle % 360;

            if (angle > 180)
            {
                angle -= 360;
            }
            else if (angle < -180)
            {
                angle += 360;
            }

            return angle;
        }
    }
}