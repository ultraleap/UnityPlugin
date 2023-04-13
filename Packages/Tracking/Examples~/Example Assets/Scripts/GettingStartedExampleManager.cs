using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GettingStartedExampleManager : MonoBehaviour
{
    enum SettingStartedState
    {
        NONE,
        STEP_1,
        STEP_2,
        STEP_3
    }

    public GameObject[] step1Objects;
    public GameObject[] step2Objects;
    public GameObject[] step3Objects;

    int remainingInteractions = 8;

    SettingStartedState currentState = SettingStartedState.NONE;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(3);
        EnableStepObjects(step1Objects);
        currentState = SettingStartedState.STEP_1;

        // AutoSkip if enough time has passed
        yield return new WaitForSeconds(60);
        remainingInteractions = 0;
        ObjectInteracted();
    }

    public void ObjectInteracted()
    {
        remainingInteractions--;

        if (currentState == SettingStartedState.STEP_1 && remainingInteractions <= 0)
        {
            DisableStepObjects(step1Objects);
            currentState = SettingStartedState.NONE;
            StartCoroutine(WaitForStep2());
        }
    }

    public void ObjectSpawned()
    {
        if (currentState == SettingStartedState.STEP_2)
        {
            currentState = SettingStartedState.STEP_3;
            StartCoroutine(WaitForStep3());
        }
    }

    IEnumerator WaitForStep2()
    {
        yield return new WaitForSeconds(3);

        EnableStepObjects(step2Objects);
        currentState = SettingStartedState.STEP_2;
    }

    IEnumerator WaitForStep3()
    {
        yield return new WaitForSeconds(3);

        EnableStepObjects(step3Objects);
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