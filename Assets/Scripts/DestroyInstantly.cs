using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DestroyInstantly : MonoBehaviour
{
    public GameObject hitParticle;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine("destroy");
    }

    // Update is called once per frame
    IEnumerator destroy()
    {
        yield return new WaitForSeconds(.1f);
        Destroy(gameObject);
    }
    
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<HandlingHit>().HandleHit();
            GameObject particle = Instantiate(hitParticle, other.transform.position, transform.rotation);
            ParticleSystem particlesys = particle.GetComponent<ParticleSystem>();
            particlesys.Play();
            
            EnemyAI ai = other.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.TakeDamage(5);
            }
        }
    }
}
