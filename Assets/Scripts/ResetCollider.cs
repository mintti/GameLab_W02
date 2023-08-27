using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetCollider : MonoBehaviour
{
    public void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            SceneManager.LoadScene("Map");
        }
    }
}
