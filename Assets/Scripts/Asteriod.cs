using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteriod : MonoBehaviour, IPlayerKillable
{
    //Internals
    protected Rigidbody _rigidbody = null;
    protected Renderer _renderer = null;
    protected Vector3 _velocity = Vector3.zero;
    protected Vector3 _rotation = Vector3.zero;

    //Awake : cache frequently used components
    protected void Awake()
    {
        //Cache renderer and rigidbody
        _rigidbody = GetComponent<Rigidbody>();
        _renderer = GetComponent<Renderer>();
        //register this asteroid with the game scene manager
        GameSceneManager._instance.AsteroidCreated();
    }
    //set properties called after an asteroid is instantiated to provide it with its operational settings
    public void SetProperties(float mass, Vector3 trajectory)
    {
        //set a random initial rotation
        transform.eulerAngles = new Vector3(Random.value * 360, Random.value * 360, Random.value * 360);

        //scale its size based on mass
        transform.localScale = Vector3.one * mass;
        //set its mass
        _rigidbody.mass = mass;
        //calculate the constant velocity we would like this asteriod to be traveling. Bigger
        //asteroids travel more slowly
        _velocity = (trajectory / mass) * GameSceneManager._instance.baseSpeed;
        //clamp speed of greater than mass speed for current level
        if (_velocity.magnitude > GameSceneManager._instance.maxSpeed)
            _velocity = _velocity.normalized * GameSceneManager._instance.maxSpeed;
        //set this as the intial velocity of rigidbody
        _rigidbody.velocity = _velocity;
        //create a vector that contains random rotations around each axis we would like to apply constantly
        _rotation = new Vector3(Random.Range(0.0f, 55), Random.Range(0.0f, 55), Random.Range(0.0f, 55));
        //slow rotation based on mass/size of asteroid
        _rotation *= 1 / mass;
    }
    public void Kill(bool byPlayer)
    {
        //now destroy ourselves
        if (GameSceneManager._instance)
        {
            GameSceneManager._instance.PlayAsteroidExplosion(transform.position, _rigidbody.mass);
            GameSceneManager._instance.AsteroidDestroyed(byPlayer?_rigidbody.mass : 0);
            Destroy(gameObject);
        }
    }
    private void Update()
    {
        transform.Rotate(_rotation*Time.deltaTime);
    }
    private void FixedUpdate()
    {
        //did we go off screen?
        if (!_renderer.isVisible)
        {
            //get our position in the world
            Vector3 position = _rigidbody.position;
            //convert the world space position into a 2d point on the screen
            Vector3 screenPos = Camera.main.WorldToScreenPoint(position);

            if(screenPos.x <0 && Vector3.Dot(_rigidbody.velocity, Vector3.right) < 0 || 
                screenPos.x > Screen.width && Vector3.Dot(_rigidbody.velocity, Vector3.right) >=0)
            {
                position.x = -position.x;
            }
            if (screenPos.y < 0 && Vector3.Dot(_rigidbody.velocity, Vector3.forward) < 0 || 
                screenPos.y > Screen.height && Vector3.Dot(_rigidbody.velocity, Vector3.forward) >= 0)
            {
                position.z = -position.z;
            }
            _rigidbody.MovePosition(position);
            //set velocity direction to head towards center of world
            _rigidbody.velocity = -_rigidbody.position;
            //set speed equal to the intial speed we set in Set Properties
            _rigidbody.velocity = _rigidbody.velocity.normalized * _velocity.magnitude;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        //have we hit another asteroid
        if(collision.gameObject.layer == LayerMask.NameToLayer("Asteroid"))
        {
            Vector3 newVelocity = collision.GetContact(0).normal;
            newVelocity.y = 0;
            newVelocity.Normalize();

            if (Mathf.Abs(newVelocity.x) < 0.4f)
            {
                newVelocity.x = 0.4f * Mathf.Sign(newVelocity.x);
                _rigidbody.velocity = newVelocity.normalized * _velocity.magnitude;
            }
            else
                 if (Mathf.Abs(newVelocity.z) < 0.4f)
            {
                newVelocity.y = 0.4f * Mathf.Sign(newVelocity.y);
                _rigidbody.velocity = newVelocity.normalized * _velocity.magnitude;
            }
            else
            {
                _rigidbody.velocity = newVelocity.normalized * _velocity.magnitude;
            }
            GameSceneManager._instance.PlayAsteroidCollision(collision.GetContact(0).point, _rigidbody.mass);
        }
        else
        if (collision.gameObject.layer==LayerMask.NameToLayer("Player Bullet") || collision.gameObject.layer==LayerMask.NameToLayer("Alien Bullet"))
        {
            if (_rigidbody.mass * 0.5f >= GameSceneManager._instance.minAsteroidMass && !collision.gameObject.CompareTag("Lethal"))
            {
                //Locals
                Vector2 randomXZ;
                Vector3 position;

                position = transform.position;
                randomXZ = Random.insideUnitCircle.normalized;
                //add offset to be parent of the first child object
                position += new Vector3(randomXZ.x, 0, randomXZ.y);

                Asteriod child1 = Instantiate(GameSceneManager._instance.randomAsteroid, position, transform.rotation);

                //Asteroid child 2
                //offset position of the 2nd child in the opposite direction by the same amount
                position = transform.position;
                position += new Vector3(-randomXZ.x, 0, -randomXZ.y);
                Asteriod child2 = Instantiate(GameSceneManager._instance.randomAsteroid, position, transform.rotation);

                randomXZ = Random.insideUnitCircle.normalized;
                Vector3 direction = new Vector3(randomXZ.x, 0.0f, randomXZ.y);

                child1.SetProperties(_rigidbody.mass * 0.5f, direction);
                child2.SetProperties(_rigidbody.mass * 0.5f, -direction);
            }
            //Tell the asteroid to kill itself
            //only pass in true to the kill function if it is the player's bullet that has destroyed us
            Kill(collision.gameObject.layer == LayerMask.NameToLayer("Player Bullet"));
        }
    }
}
