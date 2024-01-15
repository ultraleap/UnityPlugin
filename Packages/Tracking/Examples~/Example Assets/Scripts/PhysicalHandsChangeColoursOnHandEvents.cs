using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalHandsChangeColoursOnHandEvents : MonoBehaviour
{

    Renderer objectRenderer;

    [SerializeField]
    Material baseMaterial;
    [SerializeField]
    Material hoverMaterial;
    [SerializeField]
    Material contactMaterial;
    [SerializeField]
    Material grabMaterial;


    private void Start()
    {
        objectRenderer = GetComponent<Renderer>();
    }

    public void HoverEnter()
    {
        objectRenderer.material = hoverMaterial;
    }
    public void HoverExit()
    {
        objectRenderer.material = baseMaterial;
    }
    public void ContactEnter()
    {
        objectRenderer.material = contactMaterial;
    }
    public void ContactExit()
    {
        objectRenderer.material = hoverMaterial;
    }
    public void GrabEnter()
    {
        objectRenderer.material = grabMaterial;
    }
    public void GrabExit()
    {
        objectRenderer.material = contactMaterial;
    }
}
