using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class About : MonoBehaviour
{
    UIDocument aboutDoc;
    Button menuButton;

    private void OnEnable()
    {
        aboutDoc = GetComponent<UIDocument>();
        menuButton = aboutDoc.rootVisualElement.Q<Button>("MenuButton");
        menuButton.clicked += () => SceneManager.LoadScene("Menu");
    }
}
