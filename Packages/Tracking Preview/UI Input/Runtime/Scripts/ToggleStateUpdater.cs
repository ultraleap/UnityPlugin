using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleStateUpdater : MonoBehaviour
{
    [SerializeField]
    public Text ButtonText;

    [SerializeField]
    public Toggle ToggleButton;

    // Update is called once per frame
    void Update()
    {
        if (ToggleButton.isOn)
        {
            ButtonText.text = "On";
        } 
        else
        {
            ButtonText.text = "Off";
        }
    }
}
