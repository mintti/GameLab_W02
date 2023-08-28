using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
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

    [Header("키 정보")] public TextMeshProUGUI keyInfoText;
    public Dictionary<StateName, string> KeyInfoTextDict = new()
    {
        {StateName.WALK,     "점프     : SPACE BAR(A)\n" +
                             "대시     : 우클릭(L-Shoulder)\n" +
                             "스프린트 : SHIFT(L-StickPress)\n" +
                             "살금살금 : CTRL(Right-Trigger)\n" +
                             "공격     : 좌클릭(B)\n" +
                             "감도설정 : <, >(D-Pad)\n" 
        },
        {StateName.JUMP,     "백플립   : E(X)\n"+
                             "공격     : 좌클릭(B)\n"},
        {StateName.DASH,     "공격     : 좌클릭(B)\n"},
        {StateName.WALLJUMP, "공격     : 좌클릭(B)\n"},
        {StateName.BACKFLIP, "슈퍼 점프: SPACE BAR(A)"},
        {StateName.ATTACK,   "연속 공격: 좌클릭(B)"},
        {StateName.LADDER,   "JUMP     : SPACE BAR(A)"}
    };
    
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
        UpdateKeyInfo(StateName.WALK);
    }

    public void Destroy()
    {
        Destroy(gameObject);
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

    private StateName _beforeState;
    public void UpdateKeyInfo(StateName name)
    {
        if (KeyInfoTextDict.ContainsKey(name))
        {
            
            string text = KeyInfoTextDict[name];
            if (_beforeState == StateName.BACKFLIP)
            {
                text = text.Replace("점프     : SPACE BAR(A)\n", "슈퍼 점프: SPACE BAR(A)\n");
                Timer.CreateTimer(gameObject, BackflipState.backflipTime, () =>
                {
                    if (_beforeState == StateName.WALK)
                    {
                        keyInfoText.SetText(KeyInfoTextDict[_beforeState]);
                    }
                });
            }
            
            _beforeState = name;
            keyInfoText.SetText(text);
        }
    }
}
