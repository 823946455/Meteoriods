using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Alien : MonoBehaviour, IPlayerKillable
{
    //Inspector
    [SerializeField] protected float rotationSpeed = 360;
    [SerializeField] protected Bullet bulletPrefab;
    [SerializeField] protected AudioClip bulletSound;
    [SerializeField] protected AudioClip shieldSound;
    [SerializeField] protected GameObject explosion;
    [SerializeField] protected GameObject shield;

    //Internals
    protected Rigidbody _rigidbody;
    protected float nextFireTime = 0;
    protected Transform child;
    protected AudioSource audioSource;
    
    protected void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        child = transform.GetChild(0);
        audioSource = GetComponent<AudioSource>();
    }
    public void Show(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
    }
    protected void Update()
    {
        Player player = GameSceneManager._instance._player;
        if (player.isInputDisabled)
        {
            nextFireTime = Time.time + 4.0f;
        }
        else
        {
            if (Time.time > nextFireTime && GameSceneManager._instance.alienInRange)
            {
                Vector3 directionToPlayer = ((player.transform.position + player.velocity*GameSceneManager._instance.alienPrediction)-transform.position);
                float distanceToPlayer = directionToPlayer.magnitude;
                directionToPlayer.Normalize();

                Ray ray = new Ray(transform.position, directionToPlayer);
                if (!Physics.SphereCast(ray, 3, distanceToPlayer, LayerMask.NameToLayer("Asteroid")))
                    Shoot(transform.position, directionToPlayer);
            }
        }
        child.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
    protected void FixedUpdate()
    {
        Player player = GameSceneManager._instance._player;
        Vector3 direction = (GameSceneManager._instance._player.transform.position - transform.position).normalized;
        if (player.isInputDisabled)
        {
            direction = -direction;
        }
        _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, direction * GameSceneManager._instance.alienSpeed, Time.deltaTime * 8);
    }
    protected void Shoot(Vector3 position, Vector3 direction)
    {
        audioSource.PlayOneShot(bulletSound);
        Quaternion rotation = Quaternion.LookRotation(direction);
        Bullet bullet = Instantiate(bulletPrefab, position, rotation);
        bullet.SetDirection(direction);
        nextFireTime = Time.time + GameSceneManager._instance.alienFireDelay;
    }
    protected void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Player Bullet") && !collision.gameObject.CompareTag("Lethal"))
        {
            Kill(true);
        }
        else
        {
            if (collision.gameObject.CompareTag("Lethal"))
            {
                StartCoroutine(Shield());
            }
        }
    }
    public void Kill(bool byPlayer)
    {
        explosion.transform.position =  new Vector3(transform.position.x, 20, transform.position.z);
        explosion.SetActive(true);
        gameObject.SetActive(false);
        GameSceneManager._instance.AlienDestroyed();
    }
    protected IEnumerator Shield()
    {
        audioSource.PlayOneShot(shieldSound);
        shield.transform.position = new Vector3(transform.position.x, 20, transform.position.z);
        shield.SetActive(true);
        yield return null;
    }
}
