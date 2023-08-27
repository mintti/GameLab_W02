using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public Transform warpTransform;
    
    [TextArea]
    public string infoText;
    public int time;
    
    private GameObject _player;
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _player = other.gameObject;
            StartCoroutine(nameof(WarpTimer));
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StopCoroutine(nameof(WarpTimer));
            UIManager.Instance.ShowQuestion();
        }
    }

    IEnumerator WarpTimer()
    {
        int count = time;
        do
        {
            UIManager.Instance.ShowQuestion($"{count}초 후 {infoText}");
            yield return new WaitForSeconds(1f);
            count--;
        } while (count > 0);

        Warp();
    }

    private void Warp()
    {
        _player.gameObject.transform.position = warpTransform.position;
        // _player.GetComponent<PlayerController>().gameObject.transform.position = warpTransform.position;
        UIManager.Instance.ShowQuestion();
    }
}
