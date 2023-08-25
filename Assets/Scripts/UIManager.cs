using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    
    [Header("Info Text")]
    public GameObject textBoxObj;
    public TextMeshProUGUI textBoxText;
    
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
}
