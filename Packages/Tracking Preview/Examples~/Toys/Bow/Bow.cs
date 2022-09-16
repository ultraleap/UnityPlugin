using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

public class Bow : MonoBehaviour
{
    public Chirality chirality;

    public Transform bowTransform;

    public Transform arrow;

    public LineRenderer line1;
    public LineRenderer line2;

    private void OnEnable()
    {
        bowTransform.gameObject.SetActive(true);
        bowTransform.parent = null;
        bowTransform.position = transform.position;
        bowTransform.rotation = transform.rotation;

        line1.SetPosition(0, line1.transform.position);
        line2.SetPosition(0, line2.transform.position);
    }

    private void OnDisable()
    {
        bowTransform.gameObject.SetActive(false);
        line1.SetPosition(1, line1.GetPosition(0));
        line2.SetPosition(1, line2.GetPosition(0));
    }

    private void Update()
    {
        if (Hands.Get(chirality) != null)
        {
            line1.SetPosition(0, line1.transform.position);
            line2.SetPosition(0, line2.transform.position);

            line1.SetPosition(1, Hands.Get(chirality).GetPinchPosition());
            line2.SetPosition(1, Hands.Get(chirality).GetPinchPosition());

            arrow.LookAt(bowTransform);
            bowTransform.LookAt(bowTransform.position + (bowTransform.position - arrow.position));
        }
    }
}