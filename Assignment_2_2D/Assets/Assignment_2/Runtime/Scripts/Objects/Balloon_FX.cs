using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balloon_FX : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;     // The sprite renderer on this prefab
    public AudioSource audioSource;           // Audio source for the pop

    [Header("Sprites")]
    public Sprite popSprite;                 // The explosion/burst sprite
    public Sprite plusOneSprite;             // The "1+" sign sprite

    [Header("Audio")]
    public AudioClip popSound;               // The sound of the balloon popping

    [Header("Timing & Movement")]
    public float popDuration = 0.3f;         // How long to show the pop sprite
    public float floatSpeed = 1.0f;          // How fast the "1+" sprite floats up
    public float fadeSpeed = 1.0f;           // How quickly "1+" fades out

    private bool isTransitioning = false;

    void Start()
    {
        // 1) Start with the pop sprite
        if (spriteRenderer)
        {
            spriteRenderer.sprite = popSprite;
            spriteRenderer.color = Color.white; // fully opaque
        }

        if (audioSource)
        {
            audioSource.clip = popSound;
        }

        // 2) Play pop sound (if assigned)
        if (popSound && popSprite != null)
        {
            audioSource.Play();
        }

        // 3) Begin the effect coroutine
        StartCoroutine(PopSequence());
    }

    private System.Collections.IEnumerator PopSequence()
    {
        // 1) Wait popDuration seconds, showing the pop sprite
        yield return new WaitForSeconds(popDuration);

        // 2) Switch to the +1 sprite
        spriteRenderer.sprite = plusOneSprite;

        // 3) Then let it float/fade
        yield return StartCoroutine(FloatAndFade());
    }

    private System.Collections.IEnumerator FloatAndFade()
    {
        // Start from opaque
        Color c = spriteRenderer.color;
        c.a = 1f;
        spriteRenderer.color = c;

        // Keep floating up & fading out
        while (c.a > 0f)
        {
            // Move upward each frame
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;

            // Fade out
            c.a -= fadeSpeed * Time.deltaTime;
            spriteRenderer.color = c;

            yield return null; // wait 1 frame
        }

        // Once fully faded, destroy ourselves
        Destroy(gameObject);
    }
}
