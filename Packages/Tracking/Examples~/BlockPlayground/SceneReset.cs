using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReset : MonoBehaviour
{
    /// <summary>
    /// Get the active scene and reload it.
    /// </summary>
    public void ResetScene()
    {
        Scene scene = SceneManager.GetActiveScene(); 
        SceneManager.LoadScene(scene.name);
    }

    public void ExitApplication()
    {
        Application.Quit();
    }

}
