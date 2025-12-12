using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideAtRuntime : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        this.gameObject.SetActive(!Application.isPlaying);
    }
}