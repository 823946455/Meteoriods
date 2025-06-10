using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField]
    Vector3 rotationAxis = new Vector3(0,90,0);
    Transform myTransform = null;
    // Start is called before the first frame update
    void Start()
    {
        myTransform= GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        myTransform.Rotate(rotationAxis * Time.deltaTime, Space.Self);
    }
}
