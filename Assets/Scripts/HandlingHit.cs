using System;
using System.Collections;
using System.Collections.Generic;
using BarthaSzabolcs.Tutorial_SpriteFlash;
using UnityEngine;

public class HandlingHit: MonoBehaviour
{
    private Rigidbody rb;
    public List<GameObject> childObjects;

    public float hitDelay = 0f;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    // Update is called once per frame
    private void Update()
    {
        hitDelay -= Time.deltaTime;
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
}
