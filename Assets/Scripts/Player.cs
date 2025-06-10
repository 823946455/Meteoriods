using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Player : MonoBehaviour
{
    [Header("Control")]
    [SerializeField] protected float thrustAmount = 40f; //Force to add when thrust is pressed
    [SerializeField] protected float turnRate = 105f; //torque to add when turning
    [SerializeField] protected bool allowThrustReverse = false; //do we allow the reverse key
    [SerializeField] protected ParticleSystem thrusterSystem = null; //particle system used for player engines

    [Header("Weapons")]
    [SerializeField] protected Bullet bulletPrefab;
    [SerializeField] protected Bullet bulletPrefab2;
    [SerializeField] protected GameObject explosionSystem = null;
    [SerializeField] protected AudioClip blasterSound = null;

    [Header("Hyperspace")]
    [SerializeField] protected GameObject hyperspaceEffect = null;
    [SerializeField] protected AudioClip hyperspaceSound = null;
    [SerializeField] protected float hyperspaceAudioOffset = 1.1f;
    [SerializeField] protected float hyperspaceDestructionRadius = 60;
    public UnityEvent<Vector3> OnVelocity;

    [Header("Enemies")]
    [SerializeField] protected Alien alien;

    //Internals
    protected float thrustAxis; //thrust input -1 to 1
    protected float turnAxis; // turn input -1 to 1
    protected Camera _camera; //scene camera
    protected Rigidbody _rigidbody; //Rigidbody
    protected Renderer[] renderers; //all renders contain in player heirarchy
    protected bool isHyperSpacing; //are we in the process of hyperspacing?
    protected bool inputDisabled; //is player input currently disabled?
    protected AudioSource audioSource;
    protected AudioSource thrusterAudioSource;

    //Properties
    public Vector3 velocity
    {
        get { return _rigidbody.velocity; }
    }
    protected bool isVisible
    {
        get
        {
            //Iterate through child renders
            for(int i=0; i<renderers.Length; i++)
            {
                //return true if any renderer is visible
                if (renderers[i].isVisible)
                    return true;
            }
            return false;
        }
    }
    public bool isInputDisabled
    {
        get { return inputDisabled; }
    }
    //Called by unity when object is first constructed
    protected void Awake()
    {
        //Cache frequently used components for efficiency
        _camera = Camera.main;
        _rigidbody = GetComponent<Rigidbody>();
        //get all renders in sub heirarchy
        renderers = GetComponentsInChildren<Renderer>();
        //fetch and cache audio sources
        AudioSource[] audioSources = GetComponents<AudioSource>();
        audioSource = audioSources[0];
        thrusterAudioSource = audioSources[1];
    }
    //start occurs prior to the very first update
    protected void Start()
    {
        //start coroutine to put player into the level in 2 seconds
        StartCoroutine(Reset(2));
    }
    //update method
    protected void Update()
    {
        if (inputDisabled)
        {
            //cease all movement
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            //mute the thruster source audio
            thrusterAudioSource.volume = 0.0f;
            //do nothing else
            return;
        }
        //read user input for thrust and turn
        thrustAxis = Input.GetAxisRaw("Vertical");
        turnAxis = Input.GetAxisRaw("Horizontal");

        //clamp to 0 if we introduce reverse thrust
        thrustAxis = thrustAxis < 0 && !allowThrustReverse ? 0.0f : thrustAxis;

        if (Mathf.Abs(thrustAxis) > 0.1f)
        {
            thrusterAudioSource.volume = Mathf.Lerp(thrusterAudioSource.volume, 1, Time.deltaTime*15);
            //if the thruster system is not currently playing then play it
            if (!thrusterSystem.isPlaying)
            {
                thrusterSystem.Play();
            }
        }
        else
        {
            thrusterAudioSource.volume = Mathf.Lerp(thrusterAudioSource.volume, 0, Time.deltaTime * 15);
            if (thrusterSystem.isPlaying)
            {
                thrusterSystem.Stop();
            }
        }
        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }
        if (Input.GetButtonDown("Fire2") && alien.gameObject.activeInHierarchy)
        {
            Shoot2();
        }
        if (Input.GetButtonDown("Hyperspace") && !isHyperSpacing && GameSceneManager._instance.hyperspaceAvailable)
        {
            StartCoroutine(HyperSpace());
        }
    }
    //called by unity with each tick of the physics system prior to performing the physics update
    protected void FixedUpdate()
    {
        if (!isVisible)
        {
            //get our position in the world
            Vector3 position = _rigidbody.position;
            //convert the world space coordinates into a 2D point on the screen
            Vector3 screenPos = _camera.WorldToScreenPoint(position);

            if (screenPos.x < 0 && Vector3.Dot(_rigidbody.velocity, Vector3.right) < 0)
                position.x = -position.x;
            else
                if (screenPos.x > Screen.width && Vector3.Dot(_rigidbody.velocity, Vector3.right) >= 0)
                position.x = -position.x;

            if (screenPos.y < 0 && Vector3.Dot(_rigidbody.velocity, Vector3.forward) < 0)
                position.z = -position.z;
            else
                if (screenPos.y > Screen.height && Vector3.Dot(_rigidbody.velocity, Vector3.forward) >= 0)
                position.z = -position.z;
            //update the rigidbody to the new flipped position
            _rigidbody.MovePosition(position);
        }
        //is  thrust being provided
        if (thrustAxis != 0)
        {
            //apply force along the player's forward vector by the thrust amount scaled by thrustAxis
            _rigidbody.AddForce(transform.forward * thrustAxis * thrustAmount);
        }
        //is the player applying rotation
        if (turnRate != 0f)
        {
            _rigidbody.AddTorque(0, turnRate * turnAxis, 0);
        }
        OnVelocity.Invoke(_rigidbody.velocity);
    }
    protected void Shoot()
    {
        audioSource.PlayOneShot(blasterSound);
        //Instantiate a bullet and set its direction equal to where the player is facing
        Bullet bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
        bullet.SetDirection(transform.forward);
    }
    protected void Shoot2() 
    {
        audioSource.PlayOneShot(blasterSound);
        //Instantiate a bullet and set its direction equal to opposite of where player is facing
        Bullet bullet = Instantiate(bulletPrefab2, transform.position, transform.rotation);
        bullet.SetDirection(transform.forward);
    }
    protected void CollisionsEnabled(bool enable)
    {
        if (enable)
            gameObject.layer = LayerMask.NameToLayer("Player");
        else
            gameObject.layer = LayerMask.NameToLayer("Default");
    }
    //enables or disables rendering of player ship
    protected void RenderingEnabled(bool enable)
    {
        foreach(Renderer r in renderers)
        {
            r.enabled = enable;
        }
    }
    protected void OnCollisionEnter(Collision collision)
    {
        //explosion system will move to the player's position by 20 units towards the camera so it appears  in front
        explosionSystem.transform.position = new Vector3(transform.position.x, 20, transform.position.z);
        explosionSystem.SetActive(true);
        //if we have more lives left, reset the player ship for next life in 4 seconds
        if (!GameSceneManager._instance.PlayerDestroyed())
        {
            StartCoroutine(Reset(4));
        }
        else
        {
            RenderingEnabled(false);
            CollisionsEnabled(false);
            inputDisabled = true;
        }
    }
    protected void HyperSpaceEffect(bool arriving)
    {
        hyperspaceEffect.transform.position = new Vector3(transform.position.x, 20, transform.position.z);
        hyperspaceEffect.SetActive(true);

        RenderingEnabled(arriving);
        CollisionsEnabled(arriving);

        Collider[] colliders = Physics.OverlapSphere(transform.position, hyperspaceDestructionRadius, LayerMask.GetMask("Alien", "Alien Bullet", "Asteroid"));
        foreach(Collider collider in colliders)
        {
            IPlayerKillable killableThing = collider.GetComponent<IPlayerKillable>();
            if (killableThing != null)
            {
                killableThing.Kill(true);
            }
        }
    }
    protected IEnumerator HyperSpace()
    {
        isHyperSpacing = true;
        GameSceneManager._instance.HyperSpaceConsumed();
        audioSource.PlayOneShot(hyperspaceSound);
        yield return new WaitForSeconds(hyperspaceAudioOffset);
        inputDisabled = true;
        HyperSpaceEffect(false);
        while (hyperspaceEffect.activeInHierarchy)
        {
            yield return null;
        }
        yield return new WaitForSeconds(2);
        audioSource.PlayOneShot(hyperspaceSound);
        yield return new WaitForSeconds(hyperspaceAudioOffset);
        Vector2 halfScreenSize = new Vector2(Screen.width / 2, Screen.height / 2);
        Vector2 screenPos = (Random.insideUnitCircle.normalized * halfScreenSize)+halfScreenSize;
        Vector3 position = new Vector3(screenPos.x, screenPos.y, _camera.transform.position.y);

        transform.position = _camera.ScreenToWorldPoint(position)*0.75f;

        HyperSpaceEffect(true);

        inputDisabled = false;
        while (hyperspaceEffect.activeInHierarchy)
        {
            yield return null;
        }
        isHyperSpacing = false;
    }
    protected IEnumerator Reset(float delay = 0)
    {
        //disable input, renders, and colliders
        //we should be completely invisible
        inputDisabled = true;
        CollisionsEnabled(false);
        RenderingEnabled(false);
        //reposition the player at the center of the screen
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        yield return new WaitForSeconds(1.5f);

        GameSceneManager._instance.DisplayMessage("GET READY");
        yield return new WaitForSeconds(delay - hyperspaceAudioOffset);
        audioSource.PlayOneShot(hyperspaceSound);
        yield return new WaitForSeconds(hyperspaceAudioOffset);
        GameSceneManager._instance.DisplayMessage("");

        HyperSpaceEffect(true);

        inputDisabled = false;
        
    }
}
