using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneManager : MonoBehaviour
{

    static GameSceneManager instance = null;
    static public GameSceneManager _instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameSceneManager>();
            }
            return instance;
        }
    }

    private void OnDestroy()
    {
        instance = null;
    }
    //Embedded class - speed settings
    //contains settings for asteroids
    [System.Serializable]
    protected class SpeedSettings
    {
        public float baseSpeed = 13.0f; //Base speed for asteroids at game start
        public float maximumSpeed = 18.0f; //Maximum speed for asteroids at game start
        public float baseSpeedMultiplier = 1.1f; //Growth rate of Base Speed each level
        public float maximumSpeedMultiplier = 1.15f; //Growth rate  of maximum speed each level

    }
    //Embedded class - mass settings
    //contains settings for asteroids
    [System.Serializable]
    protected class MassSettings
    {
        public float minMass = 0.36f; //minimum size of an asteroid at game start
        public float maxMass = 2.6f; // Maximum size of an asteroid at game start
        public float minMassMultiplier = 0.9f; //shrink rate of minimum asteroid size each level
        public float minMassLimit = 0.18f; //smallest the asteroids can be at during the entire game
    }
    [System.Serializable]
    protected class AlienSettings
    {
        public Alien Alien; //reference to the one and only disabled alien ship on screen
        public float spawnTime = 40.0f; //frequency of alien ship appearence
        public float spawnTimeMultiplier = 0.9f; //shrink rate each level of frequency of Alien ship appearance
        public float range = 75.0f; //initial firing range to the player
        public float rangeMultiplier = 1.1f; // growth rate of firing range each level
        public float speed = 5; //initial movement speed of alien ship
        public float speedMultiplier = 1.2f; //speed multiplier per level
        public float speedLimit = 20; //limit to how fast the alien can move in the game
        public float fireDelay = 2.0f; //inital delay between firing shot at the player
        public float fireDelayMultiplier = 0.9f; //shrink rate of firing shot at player
        public float fireDelayLowLimit = 0.25f; //minimum time between shots
        public int points = 500; //points for killing an alien ship
    }
    //Inspector
    [Header("Player")]
    [SerializeField] protected Player player; //reference to player ship in scene
    [SerializeField] protected int lives = 3; //initial lives the player has at game start

    [Header("Asteriod Settings")]
    [SerializeField] protected Asteriod[] asteroids; //array of asteroid prefabs 
    [SerializeField] protected int startSpawnAmount = 7; //initial number of asteriods spawned in a scene
    [SerializeField] protected MassSettings massSettings = new MassSettings(); //mass settings for asteroids
    [SerializeField] protected SpeedSettings speedSettings = new SpeedSettings(); //speed settings for asteroids
    [SerializeField] protected AudioClip[] explosionSounds;
    [SerializeField] protected AudioClip[] collisionSounds;

    [Header("Particle Effects")]
    [SerializeField] protected GameObject asteroidExplosion = null;
    [SerializeField] protected ExplosionLight asteroidExplosionLight = null;
    [SerializeField] protected GameObject asteroidCollision = null;
    [SerializeField] protected GameObject alienShield = null;

    [Header("Alien Settings")]
    [SerializeField] protected AlienSettings alienSettings = new AlienSettings();

    [Header("UI Settings")]
    [SerializeField] protected Text messageUI;
    [SerializeField] protected Text scoreUI;
    [SerializeField] protected Text livesUI;
    [SerializeField] protected Text levelUI;
    [SerializeField] protected Text hyperSpaceUI;

    //Internals
    protected Camera _camera; //main scene camera
    protected int asteroidsRemaining; //number of asteroids remaining
    protected int currentLevel = -1; //current level
    protected int score;  //player score
    protected int nextJumpScore = 3000; //next score to reach to gain Hyperspace
    protected int jumpsAvailable; //number of hyperspace jumps available
    protected ParticleSystem[] asteroidExplosionSystems;
    protected ParticleSystem[] asteroidCollisionSystems;
    protected AudioSource _audioSource;
    protected float nextAlienSpawnTime; //next time the alien can appear
    //Properties
    //returns a reference to the player component in the scene
    public Player _player
    {
        get { return player; }
    }
    //return a random asteroid prefab from the array
    public Asteriod randomAsteroid
    {
        get { return asteroids[Random.Range(0, asteroids.Length)]; }
    }
    //get the base speed of an asteroid based on current level
    public float baseSpeed
    {
        get { return speedSettings.baseSpeed * Mathf.Pow(speedSettings.baseSpeedMultiplier, currentLevel); }
    }
    //get the max speed of an asteroid based on current level
    public float maxSpeed
    {
        get { return speedSettings.maximumSpeed * Mathf.Pow(speedSettings.maximumSpeedMultiplier, currentLevel); }
    }
    //get the minimum mass of an asteroid based on current level
    public float minAsteroidMass
    {
        get { return Mathf.Max(massSettings.minMass * Mathf.Pow(massSettings.minMassMultiplier, currentLevel), massSettings.minMassLimit); }
    }
    //get the maximum mass of an asteroid based on current level
    public float maxAsteroidMass
    {
        get { return massSettings.maxMass; }
    }
    //does the player have hyperspaces available?
    public bool hyperspaceAvailable
    {
        get { return jumpsAvailable > 0; }
    }
    public bool alienInRange
    {
        get
        {
            float distance = Vector3.Distance(player.transform.position, alienSettings.Alien.transform.position);
            float currentRange = alienSettings.range * Mathf.Pow(alienSettings.rangeMultiplier, currentLevel);
            return distance <= currentRange;
        }
    }
    public float alienSpeed
    {
        get
        {
            return Mathf.Min(alienSettings.speed * Mathf.Pow(alienSettings.speedMultiplier, currentLevel), alienSettings.speedLimit);
        }
    }
    public float alienFireDelay
    {
        get
        {
            return Mathf.Max(alienSettings.fireDelay * Mathf.Pow(alienSettings.fireDelayMultiplier, currentLevel), alienSettings.fireDelayLowLimit);
        }
    }
    public float alienPrediction
    {
        get
        {
            return Mathf.Min(currentLevel / 5.0f, 1.0f);
        }
    }
    //Start called by unity prior to first update
    protected void Start()
    {
        //cache reference to camera in scene
        _camera = Camera.main;
        //cache all particle systems that are children of the asteroid particle system and collision effects
        asteroidExplosionSystems = asteroidExplosion.GetComponentsInChildren<ParticleSystem>();
        asteroidCollisionSystems = asteroidCollision.GetComponentsInChildren<ParticleSystem>();
        //cache reference to this game's audio source
        _audioSource = GetComponent<AudioSource>();
        //set random seed
        Random.InitState(0);
        //Start a new level
        NewLevel();
    }
    protected void NewLevel()
    {
        //increment the current level
        currentLevel++;
        nextAlienSpawnTime = Time.time + alienSettings.spawnTime;
        //spawn some random asteroids
        SpawnAsteroids(startSpawnAmount);
        InvalidateUI();
        if (currentLevel != 0)
        {
            DisplayMessage("Level Complete");
            Invoke("ClearMessage", 3);
        }
        startSpawnAmount++;
    }
    protected void Update()
    {
        if (!alienSettings.Alien.gameObject.activeInHierarchy) { 
            if(Time.time >= nextAlienSpawnTime)
            {
                SpawnAlien();
            }
            else
            if (asteroidsRemaining <= 0)
            {
                NewLevel();
            }
        }
    }
    protected void SpawnAlien()
    {
        Vector2 halfScreenSize = new Vector2(Screen.width / 2, Screen.height / 2);
        Vector2 screenPos = (Random.insideUnitCircle.normalized * halfScreenSize) + halfScreenSize;
        Vector3 position = new Vector3(screenPos.x, screenPos.y, _camera.transform.position.y);
        Vector3 worldPos = _camera.ScreenToWorldPoint(position)*1.2f;
        alienSettings.Alien.Show(worldPos);
    }
    public void AlienDestroyed()
    {
        nextAlienSpawnTime = Time.time + (alienSettings.spawnTime * Mathf.Pow(alienSettings.spawnTimeMultiplier, currentLevel));
        score += alienSettings.points*(currentLevel+1);
        
        if(score > nextJumpScore)
        {
            jumpsAvailable++;
            nextJumpScore *= 2;
        }
        InvalidateUI();
    }
    protected void SpawnAsteroids(int amount)
    {
        //get half the screen size
        Vector2 halfScreenSize = new Vector2(Screen.width / 2, Screen.height / 2);
        for(int i=0; i<amount; i++)
        {
            //generate a random index in our asteroids array
            int asteroidIndex = Random.Range(0, asteroids.Length);
            //generate a random vector around the OUTSIDE of a Unit Circle then
            //multiply by half the screen size and then add half screen size. This generates
            //a random screen position around the outsideo of the screen
            Vector2 screenPos = (Random.insideUnitCircle.normalized * halfScreenSize) + halfScreenSize;
            //create a 3d vector that has the 2d screen coordinates in X and Y but has the distance 
            //from the camera in Z (needed to convert into world space with a perspective camera
            Vector3 position = new Vector3(screenPos.x, screenPos.y, _camera.transform.position.y);
            //use camera class to convert this screen position into a world position
            Vector3 worldPos = _camera.ScreenToWorldPoint(position);
            //set the direction of the asteriod to the negative of its postion so it travels
            //toward the center of the screen initially
            Vector3 trajectory = -worldPos.normalized;
            //Initiate a random asteroid instance at the world space position. We are using the 
            //random asteroidIndex we generated to fetch the asteroid prefab from the array
            Asteriod asteroid = Instantiate(asteroids[asteroidIndex], worldPos*2.4f, Quaternion.identity);

            //set the asteroid's properties so that it has a random mass/size and knows the 
            //direction in which it should travel (trajectory)
            asteroid.SetProperties(Random.Range(minAsteroidMass, maxAsteroidMass), trajectory);
        }
    }
    public void HyperSpaceConsumed()
    {
        if (jumpsAvailable > 0)
        {
            jumpsAvailable--;
            InvalidateUI();
        }
    }
    public void AsteroidCreated()
    {
        //increment the number of asteroids in the scene
        asteroidsRemaining++;
    }
    public void AsteroidDestroyed(float mass)
    {
        //decrement the number of asteroids remaining
        asteroidsRemaining--;
        //Score is based on mass/size because smaller asteroids are harder to hit. Zero will be passed if 
        //the asteroid was not destroyed by the player
        if (mass > 0)
        {
            //Multiply 1/ mass by 100. So bigger asteroids generate a smaller score
            float temp = (1.0f / mass) * 100;
            //divide by 50, force to an integer, then multiply by 50. This means the score
            //will be in increments of 50. Points awarded are either 50, 100, 150, 200, or 250 based on mass
            temp = Mathf.Ceil(temp / 50.0f) * 50;
            score += (int)temp;
            //if score is greater than the next jump score, award the player a jump and increase the next jump score
            if (score > nextJumpScore)
            {
                jumpsAvailable++;
                nextJumpScore *= 2;
            }
            //Refresh to the UI to reflect new score
            InvalidateUI();
        }
    }
    public bool PlayerDestroyed()
    {
        //decrement number of lives and Invalidate UI
        lives--;
        InvalidateUI();
        if (lives <= 0)
        {
            DisplayMessage("GAME OVER");
            Invoke("LoadMainMenu", 5);
        }
        return lives <= 0;
    }
    public void PlayAsteroidExplosion(Vector3 position, float size)
    {
        Vector3 overheadPosition = new Vector3(position.x, 20, position.z);
        asteroidExplosion.transform.position = overheadPosition;
        asteroidExplosion.transform.localScale = Vector3.one * size;

        foreach (ParticleSystem system in asteroidExplosionSystems)
        {
            system.Emit((int)system.emission.rateOverTime.constant);
        }
        asteroidExplosionLight.transform.position = overheadPosition;
        asteroidExplosionLight.ShowLight(75 * size);
        _audioSource.PlayOneShot(explosionSounds[Random.Range(0, explosionSounds.Length)], Mathf.Min(size, 1));
    }
    public void PlayAsteroidCollision(Vector3 position, float size)
    {
        Vector3 overheadPosition = new Vector3(position.x, 20, position.z);
        asteroidCollision.transform.position = overheadPosition;
        asteroidCollision.transform.localScale = Vector3.one * size;

        foreach (ParticleSystem system in asteroidCollisionSystems)
        {
            system.Emit((int)system.emission.rateOverTime.constant);
        }
        _audioSource.PlayOneShot(collisionSounds[Random.Range(0, collisionSounds.Length)], Mathf.Max(size/5, 0.45f));
    }
    public void InvalidateUI()
    {
        scoreUI.text = score.ToString();
        livesUI.text = lives.ToString();
        levelUI.text = "Level : " + (currentLevel+1).ToString();

        if (jumpsAvailable > 0 && !hyperSpaceUI.gameObject.activeInHierarchy)
        {
            hyperSpaceUI.gameObject.SetActive(true);
        }
        else
        if (jumpsAvailable == 0 && hyperSpaceUI.gameObject.activeInHierarchy)
            hyperSpaceUI.gameObject.SetActive(false);
    }
    public void DisplayMessage(string message = "")
    {
        messageUI.text = message;
    }
    public void ClearMessage()
    {
        messageUI.text = "";
    }
    protected void LoadMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}
