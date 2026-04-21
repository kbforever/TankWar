using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class Bullet : MonoBehaviour
{
    float moveSpeed = 100f;
    private Rigidbody2D rgb2D;
    float SurviveTime = 10f;
    float time = 0f;
    public Vector2 movedir = Vector2.zero;

    private string targetTag = string.Empty;
    void Awake()
    {
        time = 0f;
    }

    void Start()
    {
        rgb2D = this.GetComponent<Rigidbody2D>();
        if (this.tag == "Player") targetTag="Enemy";
        else if(this.tag=="Enemy") targetTag = "Player";
        else Debug.LogError($"{this.name}Has Else Tag!!");
       
    }
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        time+=Time.deltaTime;
        rgb2D.velocity = movedir*moveSpeed;

        if (time > SurviveTime)
        {
            Destroy(this.gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {

        if(collision.tag==this.tag) return;
        if (collision.tag == targetTag)
        {
            Debug.Log($"{collision.name} has been attacked");
        }

        Destroy(this.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log(collision.gameObject.name);
    }

    
}
