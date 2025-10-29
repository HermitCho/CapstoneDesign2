using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundScroll : MonoBehaviour
{
    [SerializeField] private float scrollSpeed;
    [SerializeField] private float posX;
    [SerializeField] private float posY;


    void Update()
    {
        transform.position += -Vector3.left * scrollSpeed * Time.deltaTime;
        transform.position += Vector3.up * scrollSpeed * Time.deltaTime;

        if(transform.position.x >= posX || transform.position.y >= posY)
        {
            transform.position = new Vector3(0, 0,0);
        }
    }
}
