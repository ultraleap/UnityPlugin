using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GettingStartedExampleManager : MonoBehaviour
{
    enum SettingStartedState
    {
        NONE,
        STEP_1,
        STEP_2
    }

    public GameObject[] step1Objects;
    public GameObject[] step2Objects;

    int remainingInteractions = 8;

    SettingStartedState currentState = SettingStartedState.NONE;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(3);
        EnableStepObjects(step1Objects);
        currentState = SettingStartedState.STEP_1;
    }

    public void ObjectInteracted()
    {
        remainingInteractions--;

        if(currentState == SettingStartedState.STEP_1 && remainingInteractions <= 0)
        {
            DisableStepObjects(step1Objects);
            currentState = SettingStartedState.NONE;
            StartCoroutine(WaitForStep2());
        }
    }

    IEnumerator WaitForStep2()
    {
        yield return new WaitForSeconds(3);

        EnableStepObjects(step2Objects);
        currentState = SettingStartedState.STEP_2;
    }

    void EnableStepObjects(GameObject[] _stepObjects)
    {
        foreach (var obj in _stepObjects)
        {
            obj.SetActive(true);
        }
    }

    void DisableStepObjects(GameObject[] _stepObjects)
    {
        foreach (var obj in _stepObjects)
        {
            obj.SetActive(false);
        }
    }
}