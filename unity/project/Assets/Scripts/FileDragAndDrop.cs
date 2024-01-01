using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using B83.Win32;
using System;
using System.Diagnostics;
using static GaussianSplatting.Editor.GaussianSplatAssetCreator;


public class FileDragAndDrop : MonoBehaviour
{

    List<string> log = new List<string>();
    void OnEnable()
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

        int iters = 7000;

        foreach (var i in aFiles)
        {
            log.Add(i);
            string args = "/k " + @"dist\conv_train\conv_train.exe " + "-s " + i + " " + "--iterations=" + iters.ToString();
            Process conv = new Process();
            conv.StartInfo.FileName = "cmd.exe";
            conv.StartInfo.Arguments = args;
            conv.Start();

            //path = "placeholder";
            //string m_InputFile = path;
            //string m_OutputFolder = @"./Resources";
            //DataQuality m_Quality = DataQuality.Medium;
            //GaussianSplatAsset.VectorFormat m_FormatPos;
            //GaussianSplatAsset.VectorFormat m_FormatScale;
            //GaussianSplatAsset.SHFormat m_FormatSH;
            //ColorFormat m_FormatColor;

            //ApplyQualityLevel();
            //CreateAsset();
        }
    }

    private void OnGUI()
    {
        if (GUILayout.Button("clear log"))
            log.Clear();
        foreach (var s in log)
            GUILayout.Label(s);
    }
}