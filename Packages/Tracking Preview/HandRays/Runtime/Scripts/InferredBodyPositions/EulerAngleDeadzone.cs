/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Preview.HandRays
{
    /// <summary>
    /// A Euler angle deadzone. Used to filter a value and stop it from changing until its delta has passed a threshold
    /// This deadzone takes into account the circular relationship between 0 and 360 for Euler Angles
    /// </summary>
    public class EulerAngleDeadzone
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

        public EulerAngleDeadzone(float deadzoneSize)
        {
            DeadzoneSize = deadzoneSize;
        }

        public EulerAngleDeadzone(float deadzoneSize, bool useRecentring, float recentreMovementSeconds, float recentreMovementThreshold, float recentreLerpSpeed)
        {
            DeadzoneSize = deadzoneSize;
            UseRecentring = useRecentring;
            RecentreMovementSeconds = recentreMovementSeconds;
            RecentreMovementThreshold = recentreMovementThreshold;
            RecentreLerpSpeed = recentreLerpSpeed;
        }

        /// <summary>
        /// How much the value needs to change before the deadzone begins to move
        /// </summary>
        public float DeadzoneSize { get; set; }

        /// <summary>
        /// If true, when still for RecentreMovementSeconds, the deadzone centre will move to the current position
        /// </summary>
        public bool UseRecentring { get; set; }

        /// <summary>
        /// If recentring is enabled, the deadzone will recentre after this amount of time
        /// </summary>
        public float RecentreMovementSeconds { get; set; }

        /// <summary>
        /// Used to check for a 'still' value
        /// </summary>
        public float RecentreMovementThreshold { get; set; }

        /// <summary>
        /// How quickly the Recentre delta returns to 0 whilst the value is changing
        /// </summary>
        public float RecentreLerpSpeed { get; set; }

        /// <summary>
        /// The centre of the deadzone
        /// </summary>
        public float DeadzoneCentre { get; private set; }

        private float recentredPositionDelta;
        private bool recentredWhilstStill;
        private Queue<TimestampDeadzoneCentre> deadzoneCentreHistory;
        private bool initialised = false;

        /// <summary>
        /// Update the deadzone based on the latest angle
        /// </summary>
        public void UpdateDeadzone(float currentAngle)
        {

            //Calculate Deadzone
            if (!initialised)
            {
                ResetDeadzone(currentAngle);
                return;
            }

            float delta = currentAngle + recentredPositionDelta - DeadzoneCentre;
            delta = SimplifyAngle(delta);

            float absDelta = Mathf.Abs(delta);

            // You've rotated too far in a single frame
            if (absDelta > 35)
            {
                ResetDeadzone(currentAngle);
                return;
            }

            if (absDelta > DeadzoneSize)
            {
                if (recentredWhilstStill)
                {
                    deadzoneCentreHistory.Clear();
                    recentredWhilstStill = false;
                }

                float signDelta = Mathf.Sign(delta);
                DeadzoneCentre += (absDelta - DeadzoneSize) * signDelta;
                DeadzoneCentre %= 360;

                //Reduce recentring offset towards 0
                if (absDelta > DeadzoneSize + 0.1f)
                {
                    recentredPositionDelta = Mathf.Lerp(recentredPositionDelta, 0, Time.deltaTime * RecentreLerpSpeed);
                }
            }

            //Calculate if need to recentre
            if (UseRecentring)
            {
                RecentreDeadzone(currentAngle);
            }
        }

        /// <summary>
        /// Resets the deadzone to the base values.
        /// </summary>
        private void ResetDeadzone(float currentAngle)
        {
            DeadzoneCentre = currentAngle;
            recentredPositionDelta = 0;
            deadzoneCentreHistory = new Queue<TimestampDeadzoneCentre>();
            initialised = true;
            recentredWhilstStill = false;
        }

        /// <summary>
        /// Move the deadzoneCentre to the current angle, if the value has changed less than RecentreMovementThreshold in the past RecentreMovementSeconds
        /// </summary>
        private void RecentreDeadzone(float currentAngle)
        {
            deadzoneCentreHistory.Enqueue(new TimestampDeadzoneCentre(Time.time, DeadzoneCentre));

            //If the earliest time in the queue was less than RecentreMovementSeconds ago, return
            if (Time.time - deadzoneCentreHistory.Peek().Timestamp < RecentreMovementSeconds)
            {
                return;
            }

            //Dequeue all times later than RecentreMovementSeconds ago
            while (Time.time - deadzoneCentreHistory.Peek().Timestamp > RecentreMovementSeconds)
            {
                deadzoneCentreHistory.Dequeue();
            }

            //If the user hasn't been relatively still for the last RecentreMovementSeconds, return
            for (int i = 0; i < deadzoneCentreHistory.Count; i++)
            {
                float recentreDelta = deadzoneCentreHistory.Peek().DeadzoneCentre - DeadzoneCentre;
                recentreDelta = SimplifyAngle(recentreDelta);
                recentreDelta = Mathf.Abs(recentreDelta);
                if (recentreDelta >= RecentreMovementThreshold)
                {
                    return;
                }
            }

            recentredWhilstStill = true;
            recentredPositionDelta = DeadzoneCentre - currentAngle;
            recentredPositionDelta = SimplifyAngle(recentredPositionDelta);
            deadzoneCentreHistory.Clear();
        }

        /// <summary>
        /// Translates our angle from the range 0-360 to -180 to +180
        /// </summary>
        /// <param name="angle">Euler angle in the range 0-360</param>
        /// <returns>Euler angle in the range -180 - 180</returns>
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