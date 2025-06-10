using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    //Inspector
    [SerializeField] protected float scrollSpeed = 1000.0f;
    //Internals
    protected Material _material;
    protected Vector3 playerVelocity;

    void Awake()
    {
        _material = GetComponent<Renderer>().material;
    }
    public void OnSetScrollSpeed(Vector3 velocity)
    {
        playerVelocity = velocity;
    }
    void Update()
    {
        Vector2 offset = _material.GetTextureOffset("_MainTex");
        offset += new Vector2((playerVelocity.x / scrollSpeed) * Time.deltaTime, (playerVelocity.z / scrollSpeed) * Time.deltaTime);
        _material.SetTextureOffset("_MainTex", offset);
    }
}
