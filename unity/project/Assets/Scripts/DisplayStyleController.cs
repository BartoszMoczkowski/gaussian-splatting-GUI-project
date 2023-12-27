//using GaussianSplatting.Runtime;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UIElements;
//using UnityEngine.SceneManagement;
//using System.IO;

//public class DisplayStyleController : MonoBehaviour
//{
//    UIDocument dropdownDoc;
//    DropdownField stylePicker;
//    DropdownField modelPicker;
//    GaussianSplatRenderer splatScene;

//    public List<string> cloudList = new List<string>();

//    private void OnEnable()
//    {
//        dropdownDoc = GetComponent<UIDocument>();

//        stylePicker = dropdownDoc.rootVisualElement.Q<DropdownField>("DisplayStylePicker");
//        modelPicker = dropdownDoc.rootVisualElement.Q<DropdownField>("ModelPicker");

//        splatScene = GameObject.Find("GaussianSplats").GetComponent<GaussianSplatRenderer>();


//    }

//    // Start is called before the first frame update
//    void Start()
//    {
//        DirectoryInfo di = new DirectoryInfo(@".\Assets\Resources");
//        foreach (var fi in di.GetFiles())
//        {
//            if (fi.Extension == ".asset")
//            {
//                cloudList.Add(Path.GetFileNameWithoutExtension(fi.Name));
//            };
//        }
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        if (Input.GetKeyDown("escape"))
//        {
//            SceneManager.LoadScene("Menu");
//        }


//        if (stylePicker.index == 1)
//        {
//            splatScene.m_RenderMode = GaussianSplatRenderer.RenderMode.DebugPoints;
//        }
//        else
//        {
//            splatScene.m_RenderMode = GaussianSplatRenderer.RenderMode.Splats;
//        }

//        for (int i = 0; i < cloudList.Count; i++)
//        {
//            if (modelPicker.index == i) 
//            {
//                splatScene.m_Asset = Resources.Load(cloudList[i]) as GaussianSplatting.Runtime.GaussianSplatAsset;
//            }
//        }
//    }
//}
