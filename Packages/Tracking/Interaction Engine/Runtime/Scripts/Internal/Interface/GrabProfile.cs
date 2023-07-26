using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "GrabProfile", menuName = "ScriptableObjects/GrabProfile", order = 1)]
public class GrabProfile : ScriptableObject
{
    [Range(0, 1)]
    public float fingerStickiness = 0F;
    [Range(0, 1)]
    public float thumbStickiness = 0.04F;
    [Range(-1, 1)]
    public float maxCurl = 0.65F;
    [Range(-1, 1)]
    public float minCurl = -0.1F;

    public float fingerRadius = 0.012F;
    public float thumbRadius = 0.017F;
    [Range(0, 1)]
    public float grabCooldown = 0.2F;
    [Range(0, 1)]
    public float maxCurlVel = 0.0F;
    [Range(-1, 0)]
    public float grabbedMaxCurlVel = -0.025F;
    [Range(0, 1)]
    public float maxGrabDistance = 0.05F;
    public int layerMask = 0;
}
