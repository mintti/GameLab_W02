using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Ladder : MonoBehaviour
{
    private bool canHorizonMove;
    private bool canVerticalMove;

    public GameObject foothold;
    public float footholdOnInterval;
    
    public bool CanHorizonMove => canHorizonMove;
    public bool CanVerticalMove => canVerticalMove;

    public bool Attach
    {
        get => _attach;
        set
        {
            _attach = value;
            if (_attach) foothold.SetActive(false);
            else Invoke(nameof(ActiveCollider), footholdOnInterval);
        }
    }

    private void ActiveCollider()
    {
        foothold.SetActive(true);
    }
    
    
    private bool _attach;
    public void Start()
    {
        foothold.SetActive(true);
    }
}
