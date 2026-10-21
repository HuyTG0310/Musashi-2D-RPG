
using UnityEngine;
using System.Collections;

public class Trap : MonoBehaviour
{
    [Header("Trap Damage")]
    public float damage = 10f;
    public bool damageOverTime = false;
    public float damageInterval = 1f;
    public LayerMask playerLayer;

    [Header("Trap Knockback")]
    public bool knockbackEnabled = true;
    public float knockbackForceX = 8f;
    public float knockbackForceY = 6f;

    [Header("Trap Slow")]
    public bool slowEnabled = true;

    [Range(0.1f, 1f)]
    public float slowMultiplier = 0.5f;

    private float originalMoveSpeed = -1f;
    private Player slowedPlayerScript;

    private Coroutine damageRoutine;
    private Player currentPlayer;
    private int playerColliderCount = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerLayer(other)) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        // Track the Player and its colliders.
        if (currentPlayer == null)
        {
            currentPlayer = player;
            playerColliderCount = 0;
        }

        if (player != currentPlayer) return;

        playerColliderCount++;

        // Apply slow only when damage over time is enabled.
        if (damageOverTime && slowEnabled)
        {
            ApplySlow(player);
        }

        // Deal damage immediately upon entering the trap.
        TryDealDamage(other);

        // Start periodic damage.
        if (damageOverTime && damageRoutine == null)
        {
            damageRoutine = StartCoroutine(
                DamageOverTimeRoutine(other)
            );
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerLayer(other)) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null || player != currentPlayer) return;

        playerColliderCount = Mathf.Max(0, playerColliderCount - 1);

        // Wait until all Player colliders have left the trap.
        if (playerColliderCount > 0) return;

        StopDamageRoutine();
        RestoreSpeed();

        currentPlayer = null;
    }

    private IEnumerator DamageOverTimeRoutine(Collider2D other)
    {
        while (true)
        {
            yield return new WaitForSeconds(
                Mathf.Max(0.01f, damageInterval)
            );

            // Stop if the Player has left the trap.
            if (currentPlayer == null) yield break;

            TryDealDamage(other);
        }
    }

    private bool IsPlayerLayer(Collider2D other)
    {
        return ((1 << other.gameObject.layer) & playerLayer) != 0;
    }

    private void TryDealDamage(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();

        if (player == null) return;

        player.TakeDamage(damage);

        // Knockback applies only to one-time damage traps.
        if (!damageOverTime && knockbackEnabled)
        {
            ApplyKnockback(other);
        }
    }
    private void ApplyKnockback(Collider2D other)
    {
        // lấy Rigidbody2D của Player trực tiếp qua Collider, không cần Player.cs phải public hoá biến rb
        Rigidbody2D playerRb = other.attachedRigidbody;
        if (playerRb == null) return;

        // xác định hướng đẩy: nếu Player đang đứng bên phải tâm trap thì đẩy sang phải, ngược lại đẩy sang trái
        float direction = (other.transform.position.x >= transform.position.x) ? 1f : -1f;

        playerRb.velocity = new Vector2(direction * knockbackForceX, knockbackForceY);
    }
    private void ApplySlow(Player player)
    {
        if (player == null) return;

        // Avoid applying the multiplier repeatedly.
        if (slowedPlayerScript == player) return;

        RestoreSpeed();

        slowedPlayerScript = player;
        originalMoveSpeed = player.moveSpeed;

        player.moveSpeed = originalMoveSpeed * slowMultiplier;
    }

    private void RestoreSpeed()
    {
        if (slowedPlayerScript != null && originalMoveSpeed >= 0f)
        {
            slowedPlayerScript.moveSpeed = originalMoveSpeed;
        }

        slowedPlayerScript = null;
        originalMoveSpeed = -1f;
    }

    private void StopDamageRoutine()
    {
        if (damageRoutine != null)
        {
            StopCoroutine(damageRoutine);
            damageRoutine = null;
        }
    }

    private void OnDisable()
    {
        StopDamageRoutine();
        RestoreSpeed();

        currentPlayer = null;
        playerColliderCount = 0;
    }

    private void ApplyKnockbackToPlayer(Collider2D other)
    {
        Rigidbody2D playerRb = other.attachedRigidbody;
        if (playerRb == null) return;

        // Xác định hướng đẩy ra xa trap.
        float direction =
            other.transform.position.x >= transform.position.x ? 1f : -1f;

        // Đẩy chéo lên trên và ra xa trap.
        playerRb.velocity = new Vector2(
            direction * knockbackForceX,
            knockbackForceY
        );
    }
}
