using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureScroller : MonoBehaviour
{
    [SerializeField] protected Vector2 scrollSpeed = Vector3.zero; //we can specify X and Y in the +/- range
    //Internals
    Material material;
    void Awake()
    {
        material = GetComponent<Renderer>().material;
    }
    void Update()
    {
        Vector2 offset = material.GetTextureOffset("_MainTex");
        material.SetTextureOffset("_MainTex", offset + (scrollSpeed * Time.deltaTime));
    }
}
