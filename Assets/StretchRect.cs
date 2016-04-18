using UnityEngine;
using System.Collections;

public class StretchRect : MonoBehaviour {

    public void Stretch(float amount)
    {
        transform.localScale = new Vector3(amount, 1f, 1f);
    }
}
