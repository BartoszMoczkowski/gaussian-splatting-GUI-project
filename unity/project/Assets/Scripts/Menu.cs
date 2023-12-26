using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    UIDocument menuDoc;
    Button startButton;
    Button generateButton;
    Button tutorialButton;
    Button aboutButton;
    Button exitButton;

    private void OnEnable()
    {
        menuDoc = GetComponent<UIDocument>();

        startButton = menuDoc.rootVisualElement.Q<Button>("StartButton");
        generateButton = menuDoc.rootVisualElement.Q<Button>("GenerateButton");
        tutorialButton = menuDoc.rootVisualElement.Q<Button>("TutorialButton");
        aboutButton = menuDoc.rootVisualElement.Q<Button>("AboutButton");
        exitButton = menuDoc.rootVisualElement.Q<Button>("ExitButton");

        startButton.clicked += () => SceneManager.LoadScene("GSTestScene");
        generateButton.clicked += () => SceneManager.LoadScene("Generate");
        tutorialButton.clicked += () => SceneManager.LoadScene("Tutorial");
        aboutButton.clicked += () => SceneManager.LoadScene("About");
        exitButton.clicked += () => Application.Quit();
    }
}
