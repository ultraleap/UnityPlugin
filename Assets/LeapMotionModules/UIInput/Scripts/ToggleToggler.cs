using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ToggleToggler : MonoBehaviour {
    public Text text;
    public Image image;
    public Color OnColor;
    public Color OffColor;

	public void SetToggle(Toggle toggle){
        if (toggle.isOn) {
            text.text = "On";
            text.color = Color.white;
            image.color = OnColor;
        } else {
            text.text = "Off";
            text.color = Color.black;
            image.color = OffColor;
        }
	}
}
