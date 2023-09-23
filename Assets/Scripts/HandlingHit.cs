using System;
using System.Collections;
using System.Collections.Generic;
using BarthaSzabolcs.Tutorial_SpriteFlash;
using UnityEngine;
using Random = UnityEngine.Random;

public class HandlingHit: MonoBehaviour
{
    private Rigidbody rb;
    public List<GameObject> childObjects;
    public GameObject hitParticle;
    public Animator anim;
    private Vector3 initialScale;
    public string name;
    [TextArea] public string HitMent;

    public float hitDelay = 0f;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialScale = transform.localScale;
    }
    
    // Update is called once per frame
    private void Update()
    {
        if (hitDelay > 0f) hitDelay -= Time.deltaTime;
    }

    public void HandleHit()
    {
        if (Random.Range(0f, 1f) <= .5f)
        {
            String Ment = "";
            switch (Random.Range(0, 6))
            {
                case 0:
                    Ment = "으악으악";
                    break;
                case 1:
                    Ment = "아푸 8ㅅ8";
                    break;
                case 2:
                    Ment = "헉헉헉헉헉";
                    break;
                case 3:
                    Ment = "맞았습니다.";
                    break;
                case 4:
                    Ment = "끼약";
                    break;
                case 5:
                    Ment = "으라차차";
                    break;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                Ment = $"{name} 「{Ment}」";
            }
            
            UIManager.Instance.ActiveInfoText(Ment);
            StartCoroutine(nameof(DeadMent), Ment);
        }
        
        if (GameObject.Find("Player") && hitDelay <= 0f)
        {
            hitDelay = .4f;
            Vector3 pos = transform.position - GameObject.Find("Player").transform.position;
            if (GetComponent<Rigidbody>())
            {
                rb.AddForce(pos.normalized * .5f, ForceMode.Impulse);
                if (gameObject.CompareTag("Enemy")) rb.AddForce(Vector3.up * 4f, ForceMode.Impulse);
            }

            if (GetComponent<SimpleFlash>())
            {
                GetComponent<SimpleFlash>().Flash();
            }
            
            foreach (GameObject child in childObjects)
            {
                child.GetComponent<SimpleFlash>().Flash();
            }
        }
    }
    
    IEnumerator DeadMent(string ment)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            yield return new WaitForSeconds(1f);
            UIManager.Instance.DeactivateInfoText(ment);
        }
        else
        {
            yield return new WaitForSeconds(1f);
            UIManager.Instance.ActiveInfoText($"{name}가 말했다.");
            yield return new WaitForSeconds(1f);
            UIManager.Instance.DeactivateInfoText($"{name}가 말했다.");
        }
    }
    

    public void handlingZzibu()
    {
        //Create Particle
        GameObject particle = Instantiate(hitParticle, transform.position, transform.rotation);
        ParticleSystem particlesys = particle.GetComponent<ParticleSystem>();
        particlesys.Play();
        anim.SetTrigger("Zzibu");
        StartCoroutine("Jump");
        HandleHit();
    }

    IEnumerator Jump()
    {
        yield return new WaitForSeconds(2.8f);
        rb.AddForce(Vector3.up * 30000, ForceMode.Impulse);
    }
    
    public void AdjustScaleEvent()
    {
        Debug.Log("AdjustScaleEvent");
        transform.localScale = initialScale;
    }
}
