using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialInitilizer : MonoBehaviour
{

    [SerializeField] private Material[] blockMaterials = null;
    [SerializeField] private Material[] ballMaterials = null;
    [SerializeField] private Material[] plankMaterials = null;

    [SerializeField] private Renderer ballRenderer = null;
    [SerializeField] private Renderer plankRenderer = null;
    [SerializeField] private GameObject block = null;


    void Start()
    {
        ballRenderer.material = ballMaterials[0];
        plankRenderer.material = plankMaterials[0];
        changeBlockMaterial(block);
    }


    public void changeBlockMaterial(GameObject block)
    {

        Renderer[] renderers = block.GetComponentsInChildren<Renderer>();
        foreach (var ren in renderers)
        {
            ren.material = blockMaterials[0];
        }

    }

}
