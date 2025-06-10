using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] protected Image fade = null; //black full screen UI image to use for fade in / fade out
    [SerializeField] protected float fadeSpeed = 1.5f; //speed to fade
    //Internals
    protected AudioSource audioSource; //audio source on this gameobject with music clip assigned
    protected float audioVolume; //used to cache intial volume

    protected void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        fade.color = new Color(0, 0, 0, 1);

        audioSource = GetComponent<AudioSource>();
        audioVolume = audioSource.volume;

        StartCoroutine(Fade(true, fadeSpeed));
    }
    IEnumerator Fade(bool fadeIn, float speed)
    {
        Color targetColor = fadeIn ? new Color(0, 0, 0, 0) : new Color(0, 0, 0, 1);
        Color sourceColor = fadeIn ? new Color(0, 0, 0, 1) : new Color(0, 0, 0, 0);

        float timer = 0;
        while(timer <= fadeSpeed)
        {
            fade.color = Color.Lerp(sourceColor, targetColor, timer / speed);
            if (fadeIn)
            {
                audioSource.volume = Mathf.Lerp(0, audioVolume, timer / speed);
            }
            else
            {
                audioSource.volume = Mathf.Lerp(audioVolume, 0, timer / speed);
            }
            timer += Time.deltaTime;
            yield return null;
        }
        fade.color = targetColor;
    }
    public void NewGame()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        StartCoroutine(Fade(false, fadeSpeed/2));
        Invoke("LoadGameScene", fadeSpeed/2);
    }
    protected void LoadGameScene()
    {
        SceneManager.LoadScene("Main Game");
    }
    public void QuitGame()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        StartCoroutine(Fade(false, fadeSpeed / 2));
        Invoke("LoadCredits", fadeSpeed / 2);
    }
    protected void LoadCredits()
    {
        SceneManager.LoadScene("Closing Credits");
    }
}
