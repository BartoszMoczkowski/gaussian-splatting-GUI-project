using GaussianSplatting.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.IO;

public class DisplayStyleController : MonoBehaviour
{
    UIDocument uidoc;
    DropdownField stylePicker;
    DropdownField modelPicker;
    GaussianSplatRenderer splatScene;

    FlyCamera cameraScript;
    GameObject container;

    public List<string> cloudList = new List<string>();
    int prev_i = -1;

    private void OnEnable()
    {
        cameraScript = GameObject.Find("Main Camera").GetComponent<FlyCamera>();
        container = GameObject.Find("UIContainer");

        uidoc = GameObject.Find("UIDocument").GetComponent<UIDocument>();

        stylePicker = uidoc.rootVisualElement.Q<DropdownField>("DisplayStylePicker");
        modelPicker = uidoc.rootVisualElement.Q<DropdownField>("ModelPicker");

        splatScene = GameObject.Find("GaussianSplats").GetComponent<GaussianSplatRenderer>();


    }

    void Start()
    {
        //DirectoryInfo di = new DirectoryInfo(@".\Resources");
        DirectoryInfo di = new DirectoryInfo(@".\Assets\Resources");
        foreach (var fi in di.GetFiles())
        {
            if (fi.Extension == ".json")
            {
                cloudList.Add(Path.GetFileNameWithoutExtension(fi.Name));
            };
        }
    }

    void Update()
    {
        if (Input.GetKeyDown("m"))
        {
            SceneManager.LoadScene("Menu");
        }

        if (Input.GetKeyDown("h"))
        {
            cameraScript.enabled = true;
            container.SetActive(false);
        }


        if (stylePicker.index == 1)
        {
            splatScene.m_RenderMode = GaussianSplatRenderer.RenderMode.DebugPoints;
        }
        else
        {
            splatScene.m_RenderMode = GaussianSplatRenderer.RenderMode.Splats;
        }


        
        if (prev_i == modelPicker.index)
        {
            return;
        }
        for (int i = 0; i < cloudList.Count; i++)
        {
            if (modelPicker.index == i) 
            {
                //string dir = @".\Resources\";
                string dir = @"Assets\Resources\";
                GaussianSplatAsset asset_temp = ScriptableObject.CreateInstance<GaussianSplatAsset>();

                
                
                string json_path =  dir + cloudList[i]+".json";
                JsonUtility.FromJsonOverwrite(File.ReadAllText(json_path),asset_temp);

               
                string pathChunk = $"{cloudList[i]}_chk";
                string pathPos = $"{cloudList[i]}_pos";
                string pathOther = $"{cloudList[i]}_oth";
                string pathCol = $"{cloudList[i]}_col";
                string pathSh = $"{cloudList[i]}_shs";

                asset_temp.m_ChunkData = Resources.Load<TextAsset>(pathChunk);
                asset_temp.m_PosData = Resources.Load<TextAsset>(pathPos);
                asset_temp.m_OtherData = Resources.Load<TextAsset>(pathOther);
                asset_temp.m_ColorData = Resources.Load<TextAsset>(pathCol);
                asset_temp.m_SHData = Resources.Load<TextAsset>(pathSh);


                splatScene.m_Asset= asset_temp ;
                prev_i= i;

                //splatScene.m_Asset = Resources.Load(cloudList[i]) as GaussianSplatting.Runtime.GaussianSplatAsset;
            }
        }
    }
}
