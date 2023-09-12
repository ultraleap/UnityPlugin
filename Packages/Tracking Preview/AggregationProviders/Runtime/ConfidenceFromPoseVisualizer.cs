using Leap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfidenceFromPoseVisualizer : MonoBehaviour
{
    [SerializeField]
    public UnityEngine.UI.RawImage outputImage;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateTexture(RenderTexture tex)
    {
        outputImage.texture = tex;
    }
}
