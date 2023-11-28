using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalHandsHandMenu : MonoBehaviour
{
    [SerializeField]
    List<GameObject> ExpandingButtons = new List<GameObject>();
    Vector3 buttonScale = new Vector3();

    // Start is called before the first frame update
    void Start()
    {
        foreach (var button in ExpandingButtons)
        {
            button.SetActive(false);
            buttonScale = button.transform.localScale;
        }
    }

    public void EnableButtons()
    {
        foreach (var button in ExpandingButtons)
        {
            //var coroutine = ScaleButton(button, buttonScale, Vector3.zero);
            //StartCoroutine(coroutine);
            button.SetActive(true);
        }
    }

    public void DissableButtons()
    {
        foreach (var button in ExpandingButtons)
        {
            //var coroutine = ScaleButton(button, Vector3.zero, buttonScale);
            //StartCoroutine(coroutine);
            button.SetActive(false);
        }
    }

    private IEnumerator ScaleButton(GameObject button, Vector3 targetScale, Vector3 startScale)
    {
        button.transform.localScale = startScale;

        float timeTakes = 1f; // animation will take one second
        float elapsedTime = 0;

        while (elapsedTime < timeTakes)
        {
            button.transform.localScale = Vector3.Lerp(button.transform.localScale, targetScale, (elapsedTime / timeTakes));

            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }
}
