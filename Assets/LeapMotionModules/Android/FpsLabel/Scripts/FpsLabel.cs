using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Leap.Unity {
  public class FpsLabel : MonoBehaviour {
    public LeapProvider Provider;
  	public Text frameRateText;
    private float fps;

  	void Update () {
  	  Frame frame = Provider.CurrentFrame;
  	  float interp = Time.deltaTime / (0.5f + Time.deltaTime);
  	  float currentFPS = 1.0f / Time.deltaTime;
  	  fps = Mathf.Lerp (fps, currentFPS, interp);
  	  frameRateText.text = "Data FPS:" + frame.CurrentFramesPerSecond.ToString ("f2") +
  		System.Environment.NewLine + "Render FPS:" + Mathf.RoundToInt (fps).ToString ("f2");
  	}
  }
}
