/******************************************************************************\
* Copyright (C) Leap Motion, Inc. All rights reserved.               *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.SceneManagement;
using Leap;

public class VisualizerManager : MonoBehaviour {
  public GameObject m_PCVisualizer = null;
  public GameObject m_VRVisualizer = null;
  public UnityEngine.UI.Text m_modeText;
  public UnityEngine.UI.Text m_warningText;

  private Controller m_controller = null;
  private bool m_leapConnected = false;

  private void FindController()
  {
    LeapProvider provider = GameObject.FindObjectOfType<LeapProvider>() as LeapProvider;
    if (provider != null)
      m_controller = provider.GetLeapController();
  }

  void Awake()
  {
    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, false);
    if (VRDevice.isPresent)
    {
      m_PCVisualizer.gameObject.SetActive(false);
      m_VRVisualizer.gameObject.SetActive(true);
      m_modeText.text = "VR Mode";
      m_warningText.text = "Please put on your head-mounted display";
    }
    else
    {
      m_VRVisualizer.gameObject.SetActive(false);
      m_PCVisualizer.gameObject.SetActive(true);
      m_modeText.text = "Desktop Mode";
      m_warningText.text = "Orion is built for virtual reality and performs best when head-mounted";
    }
  }

  void Start()
  {
    FindController();
    if (m_controller != null)
      m_leapConnected = m_controller.IsConnected;
  }

  void Update()
  {
    if (m_leapConnected)
      return;

    if (m_controller == null)
    {
      FindController();
    } else
    {
      if (m_controller.IsConnected)
      {
        // HACK (wyu): LeapProvider should listen to events and update itself when Leap devices are connected/disconnected instead of having to reload the scene to reinitialize variables
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
      }
    }
  }
}
