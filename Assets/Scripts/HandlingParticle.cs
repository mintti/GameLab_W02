using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HandlingParticle : MonoBehaviour
{
    public GameObject hitParticle;

    private bool hitOnce = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.transform.CompareTag("Enemy") && hitOnce == true)
        { 
            hitOnce = false;
            other.GetComponent<HandlingHit>().HandleHit();
            GameObject particle = Instantiate(hitParticle, other.transform.position, transform.rotation);
            ParticleSystem particlesys = particle.GetComponent<ParticleSystem>();
            particlesys.Play();
        }
        
        if (other.transform.CompareTag("FlyingEnemy") && hitOnce == true)
        { 
            hitOnce = false;
            other.GetComponent<HandlingHit>().HandleHit();
            GameObject particle = Instantiate(hitParticle, other.transform.position, transform.rotation);
            ParticleSystem particlesys = particle.GetComponent<ParticleSystem>();
            particlesys.Play();
        }
    }
}
