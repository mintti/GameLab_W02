using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EasterEgg : MonoBehaviour
{
    public string name;
    [TextArea] public string deadMent;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadLine"))
        {
            UIManager.Instance.ActiveInfoText($"{name} 「{deadMent}」");
            StartCoroutine(nameof(DeadMent));
        }
    }

    

    IEnumerator DeadMent()
    {
        yield return new WaitForSeconds(3f);
        UIManager.Instance.ActiveInfoText($"{name}의 단말마가 울려 퍼졌다.");
        yield return new WaitForSeconds(1f);
        UIManager.Instance.DeactivateInfoText($"{name}의 단말마가 울려 퍼졌다.");
    }
}
