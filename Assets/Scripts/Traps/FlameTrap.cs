
using UnityEngine;
using System.Collections;

public class FlameTrap : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string idleState = "Trap_Idle";
    public string fireState = "Trap_Fire";

    [Header("Fire Hitbox")]
    public BoxCollider2D flameHitbox;
    public LayerMask playerLayer;

    [Header("Damage")]
    public float damage = 10f;

    [Header("Trap Timing")]
    public float idleDuration = 2f;
    public float fireDuration = 1.5f;

    private Coroutine trapRoutine;

    private bool isFiring = false;
    private bool playerInside = false;
    private bool hasDamagedPlayerThisFire = false;

    private Player currentPlayer;


    public AudioSource trapAudioSource;
    public AudioClip fireSound;

    private void Start()
    {
        // Get Animator automatically if not assigned.
        if (animator == null)
            animator = GetComponent<Animator>();

        // Get BoxCollider2D automatically if not assigned.
        if (flameHitbox == null)
            flameHitbox = GetComponent<BoxCollider2D>();

        // Validate required components.
        if (animator == null)
        {
            Debug.LogError("FlameTrap: Animator is missing!", this);
            enabled = false;
            return;
        }

        if (flameHitbox == null)
        {
            Debug.LogError("FlameTrap: BoxCollider2D is missing!", this);
            enabled = false;
            return;
        }

        // Configure the hitbox.
        flameHitbox.isTrigger = true;
        flameHitbox.enabled = false;

        // Start the trap cycle.
        trapRoutine = StartCoroutine(TrapRoutine());
    }

    private IEnumerator TrapRoutine()
    {
        while (true)
        {
            // Idle phase.
            isFiring = false;
            flameHitbox.enabled = false;

            animator.Play(idleState, 0, 0f);

            yield return new WaitForSeconds(idleDuration);

            // Fire phase.
            isFiring = true;
            hasDamagedPlayerThisFire = false;

            flameHitbox.enabled = true;
            animator.Play(fireState, 0, 0f);

            // Damage the Player if already inside the hitbox.
            if (playerInside)
                DealDamageOnce();

            yield return new WaitForSeconds(fireDuration);

            // End the fire phase.
            isFiring = false;
            flameHitbox.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only detect the Player layer.
        if (!IsPlayerLayer(other))
            return;

        Player player = other.GetComponentInParent<Player>();

        if (player == null)
            return;

        currentPlayer = player;
        playerInside = true;

        // Damage once if entering during the fire phase.
        if (isFiring)
            DealDamageOnce();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerLayer(other))
            return;

        Player player = other.GetComponentInParent<Player>();

        if (player == null || player != currentPlayer)
            return;

        playerInside = false;
        currentPlayer = null;
    }

    private bool IsPlayerLayer(Collider2D other)
    {
        return (playerLayer.value &
                (1 << other.gameObject.layer)) != 0;
    }

    private void DealDamageOnce()
    {
        // Only deal damage during the fire phase.
        if (!isFiring)
            return;

        // Prevent repeated damage during the same fire phase.
        if (hasDamagedPlayerThisFire)
            return;

        if (currentPlayer == null)
            return;

        // Mark as damaged before calling TakeDamage.
        hasDamagedPlayerThisFire = true;

        currentPlayer.TakeDamage(damage);

        Debug.Log(
            "FlameTrap dealt " + damage +
            " damage to Player."
        );
    }

    private void OnDisable()
    {
        // Stop the trap cycle.
        if (trapRoutine != null)
        {
            StopCoroutine(trapRoutine);
            trapRoutine = null;
        }

        isFiring = false;
        playerInside = false;
        currentPlayer = null;

        // Disable the hitbox.
        if (flameHitbox != null)
            flameHitbox.enabled = false;
    }



    private void PlayFireSound()
    {
        if(trapAudioSource != null &&  fireSound != null)
        {
            trapAudioSource.PlayOneShot(fireSound);
        }
    }
}
