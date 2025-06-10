using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour, IPlayerKillable
{
    //Inspector
    [SerializeField] protected float speed = 500f; //speed of bullet
    [SerializeField] protected Rigidbody _rigidbody = null; //hook up via inspector for efficiency
    [SerializeField] protected int lifeDuration = 5; //how long should a bullet live before being destroyed
    public void Kill(bool byPlayer)
    {
        //Destroy this game object from the scene
        Destroy(gameObject);
    }
    public void SetDirection(Vector3 direction)
    {
        _rigidbody.AddForce(direction * speed, ForceMode.Impulse);
        //destroy the game object when its life duration expires
        Destroy(gameObject, lifeDuration);
    }
    private void OnCollisionEnter(Collision collision)
    {
        Kill(true);
    }
}
