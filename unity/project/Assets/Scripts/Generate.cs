using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Generate : MonoBehaviour
{
    UIDocument generateDoc;
    Button menuButton;

    private void OnEnable()
    {
        generateDoc = GetComponent<UIDocument>();
        menuButton = generateDoc.rootVisualElement.Q<Button>("MenuButton");
        menuButton.clicked += () => SceneManager.LoadScene("Menu");
    }
}
