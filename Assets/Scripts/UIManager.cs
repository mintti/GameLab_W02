using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    
    [Header("Info Text")]
    public GameObject textBoxObj;
    public TextMeshProUGUI textBoxText;

    [Header("Question Related")] 
    public GameObject questionObj;
    public Text questionText;
    private Action _questionCallback; 
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        textBoxObj.SetActive(false);
        questionObj.SetActive(false);
    }

    public void ActiveInfoText(string text)
    {
        textBoxObj.SetActive(true);
        textBoxText.SetText(text);
    }

    public void DeactivateInfoText(string text)
    {
        if (textBoxText.text == text)
        {
            textBoxObj.SetActive(false);
        }
        else
        {
            // 다른 텍스트가 출력 중이다.
        }
    }

    public void ShowQuestion(string text = null)
    {
        bool active = !string.IsNullOrEmpty(text);
        questionObj.SetActive(active);
        questionText.text = text;
    }
}
