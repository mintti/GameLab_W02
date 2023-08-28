using System;
using System.Collections;
using System.Collections.Generic;
using BarthaSzabolcs.Tutorial_SpriteFlash;
using UnityEditor.Animations;
using UnityEngine;

public class HandlingHit: MonoBehaviour
{
    private Rigidbody rb;
    public List<GameObject> childObjects;
    public GameObject hitParticle;
    public Animator anim;
    private Vector3 initialScale;

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
        if (GameObject.Find("Player") && hitDelay <= 0f)
        {
            hitDelay = .4f;
            Vector3 pos = transform.position - GameObject.Find("Player").transform.position;
            rb.AddForce(pos.normalized * .5f, ForceMode.Impulse);
            if (gameObject.CompareTag("Enemy"))rb.AddForce(Vector3.up * 4f, ForceMode.Impulse);
            
            foreach (GameObject child in childObjects)
            {
                child.GetComponent<SimpleFlash>().Flash();
            }
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
