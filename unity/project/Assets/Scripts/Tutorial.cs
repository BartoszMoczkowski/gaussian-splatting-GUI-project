using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Tutorial: MonoBehaviour
{
    UIDocument tutorialDoc;
    Button menuButton;

    private void OnEnable()
    {
        tutorialDoc = GetComponent<UIDocument>();
        menuButton = tutorialDoc.rootVisualElement.Q<Button>("MenuButton");
        menuButton.clicked += () => SceneManager.LoadScene(0);
    }
}
