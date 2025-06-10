using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuShipRotator : MonoBehaviour
{
    [SerializeField] protected AnimationCurve xRotationCurve;
    [SerializeField] protected float speed = 0;
    [SerializeField] protected float scale = 10;

    //internals
    protected Vector3 initialRotation;
    protected float time = 0;

    void Awake()
    {
        initialRotation = transform.localEulerAngles;
    }
    void Update()
    {
        transform.localEulerAngles = new Vector3(initialRotation.x + (xRotationCurve.Evaluate(time % 6) * scale), initialRotation.y, initialRotation.z);

        time += Time.deltaTime * speed;
    }
}
