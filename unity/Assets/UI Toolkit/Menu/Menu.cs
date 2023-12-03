using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    UIDocument menuDoc;
    Button startButton;
    Button exitButton;

    private void OnEnable()
    {
        menuDoc = GetComponent<UIDocument>();

        startButton = menuDoc.rootVisualElement.Q<Button>("StartButton");
        exitButton = menuDoc.rootVisualElement.Q<Button>("ExitButton");

        startButton.clicked += () => SceneManager.LoadScene("GSTestScene");
        startButton.clicked += () => Application.Quit();
    }

}
