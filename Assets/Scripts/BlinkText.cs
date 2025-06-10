using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlinkText : MonoBehaviour
{
    //Inspector
    [SerializeField] protected float blinkTime = 0.75f;
    //Internals
    Text textToBlink; //text element to enable/disable during blinking
    float nextToggleTime;//next time to toggle text visibility
    private void Awake()
    {
        //cache a reference to the text component on this object
        textToBlink = GetComponent<Text>();
    }
    private void OnEnable()
    {
        textToBlink.enabled = true;
        nextToggleTime = Time.deltaTime + blinkTime;
    }
    void Update()
    {
        if (Time.time >= nextToggleTime)
        {
            textToBlink.enabled = !textToBlink.enabled;
            nextToggleTime = Time.time + blinkTime;
        }
    }
}
