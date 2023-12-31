using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using B83.Win32;
using System;
using System.Diagnostics;


public class FileDragAndDrop : MonoBehaviour
{
    //string m_path = Application.dataPath;
    //string g_path = Environment.CurrentDirectory;


    List<string> log = new List<string>();
    void OnEnable ()
    {
        // must be installed on the main thread to get the right thread id.
        UnityDragAndDropHook.InstallHook();
        UnityDragAndDropHook.OnDroppedFiles += OnFiles;
    }
    void OnDisable()
    {
        UnityDragAndDropHook.UninstallHook();
    }

    void OnFiles(List<string> aFiles, Vector2 aPos)
    {
        // do something with the dropped file names. aPos will contain the 
        // mouse position within the window where the files has been dropped.
        string str = "Dropped " + aFiles.Count + " files at: " + aPos + "\n\t" +
            aFiles.Aggregate((a, b) => a + "\n\t" + b);
       /// UnityEngine.Debug.Log(m_path);
        //UnityEngine.Debug.Log(g_path);
        UnityEngine.Debug.Log(str);
        log.Add(str);
        Process.Start(@"..\..\dist\conv_train\conv_train.exe -" + str);
    }

    private void OnGUI()
    {
        if (GUILayout.Button("clear log"))
            log.Clear();
        foreach (var s in log)
            GUILayout.Label(s);
    }
}
