using GaussianSplatting.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DisplayStyleController: MonoBehaviour
{
    UIDocument dropdownDoc;
    DropdownField stylePicker;
    DropdownField modelPicker;
    GaussianSplatRenderer splatScene;

    private void OnEnable()
    {
        dropdownDoc = GetComponent<UIDocument>();

        stylePicker = dropdownDoc.rootVisualElement.Q<DropdownField>("DisplayStylePicker");
        modelPicker = dropdownDoc.rootVisualElement.Q<DropdownField>("ModelPicker");

        splatScene = GameObject.Find("GaussianSplats").GetComponent<GaussianSplatRenderer>();

        
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(stylePicker.index == 1)
        {
            splatScene.m_RenderMode = GaussianSplatRenderer.RenderMode.DebugPoints;
        }
        else
        {
            splatScene.m_RenderMode = GaussianSplatRenderer.RenderMode.Splats;
        }

        if (modelPicker.index == 1)
        {
            splatScene.m_Asset = Resources.Load("bicycle-point_cloud-iteration_30000-point_cloud") as GaussianSplatting.Runtime.GaussianSplatAsset;
        }
        else
        {
            splatScene.m_Asset = Resources.Load("flowers-point_cloud-iteration_30000-point_cloud") as GaussianSplatting.Runtime.GaussianSplatAsset;
        }

        

    }
}
