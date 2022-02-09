using Leap.Unity;
using System.Collections.Generic;
using UnityEngine;

namespace Ultraleap.Tracking.OpenXR
{
    public class HandJointVisualizer : MonoBehaviour
    {
        private readonly List<GameObject> _leftJoints = new List<GameObject>();
        private readonly List<GameObject> _rightJoints = new List<GameObject>();

        private void Start()
        {
            GameObject o = gameObject;
            for (var i = 0; i < HandTracker.Left.JointCount; ++i)
            {
                var jointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(jointSphere.GetComponent<Collider>());
                _leftJoints.Add(jointSphere);
                jointSphere.transform.parent = o.transform;
                jointSphere.SetActive(false);
            }

            for (var i = 0; i < HandTracker.Right.JointCount; ++i)
            {
                var jointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(jointSphere.GetComponent<Collider>());
                _rightJoints.Add(jointSphere);
                jointSphere.transform.parent = o.transform;
                jointSphere.SetActive(false);
            }
        }

        private void Update()
        {
            var leftHandJoints = new HandJointLocation[HandTracker.Left.JointCount];
            if (HandTracker.Left.TryLocateHandJoints(FrameTime.OnUpdate, leftHandJoints))
            {
                foreach (var joint in leftHandJoints)
                {
                    _leftJoints[(int)joint.JointId].SetActive(true);
                    _leftJoints[(int)joint.JointId].transform.SetPose(joint.Pose);
                    _leftJoints[(int)joint.JointId].transform.localScale = Vector3.one * joint.Radius;
                }
            }
            else
            {
                _leftJoints.ForEach(jointSphere => jointSphere.SetActive(false));
            }

            var rightHandJoints = new HandJointLocation[HandTracker.Right.JointCount];
            if (HandTracker.Right.TryLocateHandJoints(FrameTime.OnUpdate, rightHandJoints))
            {
                foreach (var joint in rightHandJoints)
                {
                    _rightJoints[(int)joint.JointId].SetActive(true);
                    _rightJoints[(int)joint.JointId].transform.SetPose(joint.Pose);
                    _rightJoints[(int)joint.JointId].transform.localScale = Vector3.one * joint.Radius;
                }
            }
            else
            {
                _rightJoints.ForEach(jointSphere => jointSphere.SetActive(false));
            }
        }
    }
}