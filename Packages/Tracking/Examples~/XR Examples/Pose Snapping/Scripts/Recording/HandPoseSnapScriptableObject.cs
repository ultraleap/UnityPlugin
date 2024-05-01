using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPoseSnapScriptableObject : ScriptableObject
{
    public Chirality chirality;
    public HandPoseScriptableObject handPose;
    public Pose poseToObjectOffset;
}