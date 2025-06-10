using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CreditsSceneManager : MonoBehaviour
{
    //Inspector
    [SerializeField] protected RectTransform creditsContainer; //the rect transform of the container we wish to scroll
    [SerializeField] protected Image fade; //black square image component for fade-out effect
    [SerializeField] protected float finalScrollPos; //the final y position we want the container to be at
    [SerializeField] protected float creditsDuration; //how fast the credits are going to be scrolling
    [SerializeField] protected float fadeDuration; //how long music and fade effects should take

    //Internals
    protected AudioSource audioSource; //audio source playing the music
    protected float audioVolume; //used to cache inital volume of audio volume we would like to fade into
    protected Vector3 initialScrollPos;

    //Start
    void Start()
    {
        //cache audio source component
        audioSource = GetComponent<AudioSource>();
        //assume current volume of audio source is volume we want to fade into
        audioVolume = audioSource.volume;
        audioSource.volume = 0;

        initialScrollPos = creditsContainer.localPosition;
        StartCoroutine(CreditRoll());
        StartCoroutine(Music());
    }
    IEnumerator CreditRoll()
    {
        float timer = 0;
        bool canSkip = PlayerPrefs.GetInt("Can Skip Credits", 0)>0;
        PlayerPrefs.SetInt("Can Skip Credits", 1);

        while(timer <= creditsDuration)
        {
            Vector3 containerPos = initialScrollPos;
            containerPos.y = Mathf.Lerp(initialScrollPos.y, finalScrollPos, timer / creditsDuration);
            creditsContainer.localPosition = containerPos;
            timer += Time.deltaTime;

            if (Input.anyKeyDown && canSkip)
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
            yield return null;
        }
    }
    IEnumerator Music()
    {
        //set timer to zero
        float timer = 0;
        while (timer <= fadeDuration)
        {
            //lerp audio source volume for zero to its initial cached volume over the fade duration
            audioSource.volume = Mathf.Lerp(0, audioVolume, timer / fadeDuration);
            //accumulate time
            timer += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(creditsDuration - fadeDuration);

        timer = 0;
        Color fadeColor = fade.color;
        while (timer <= fadeDuration)
        {
            audioSource.volume = Mathf.Lerp(0, audioVolume, 1 - (timer / fadeDuration));
            fadeColor.a = timer / fadeDuration;
            fade.color = fadeColor;

            timer += Time.deltaTime;

            yield return null;
        }
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
