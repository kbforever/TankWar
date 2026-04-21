using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class Bullet : MonoBehaviour
{
    float moveSpeed = 500f;
    private Rigidbody2D rgb2D;

    public Vector2 movedir = Vector2.zero;

    private string targetTag = string.Empty;
    void Awake()
    {
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
        // time+=Time.deltaTime;
        

        // if (time > SurviveTime)
        // {
        //     Destroy(this.gameObject);
        // }
    }

    void FixedUpdate()
    {
        rgb2D.velocity = movedir*moveSpeed;
    }


    void OnTriggerEnter2D(Collider2D collision)
    {

        if(collision.tag==this.tag) return;
        if (collision.tag == targetTag)
        {
            collision.gameObject.GetComponent<ITakeDamage>()?.TakeDamage(1);
        }

        Destroy(this.gameObject);
    }


    
}
