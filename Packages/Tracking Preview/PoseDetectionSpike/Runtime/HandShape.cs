using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandShape : MonoBehaviour
{

    public PoseDetectionRotationToLastBone poseDetectionRecording;
    public PoseDetectionCombineErrors poseDetectionCombineErrors;

    [HideInInspector]
    public Vector3[] jointPositions;

    public enum HandShapeEditorModes
    {
        rotateBones,
        editThresholds
        //tryValidPoses
    }
    public HandShapeEditorModes currentEditorMode = HandShapeEditorModes.rotateBones;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
