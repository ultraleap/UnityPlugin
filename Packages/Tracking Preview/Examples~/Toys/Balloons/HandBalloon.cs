using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandBalloon : MonoBehaviour
{
    private void Update()
    {
        transform.forward = transform.position + Vector3.up;
    }
}