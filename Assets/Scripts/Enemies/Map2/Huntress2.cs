using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Enemies.Map2
{
    public class Huntress2 : EnemyBase
    {
        [Header("Huntress AI & Combat")]
        public float chaseRange = 12f;      // Tầm nhìn phát hiện Musashi
        public float attackRange = 7.0f;    // Tầm bắn cung từ xa
        public float attackCooldown = 1.6f; // Thời gian hồi chiêu
        public float attackDamage = 28f;    // Sát thương đòn đánh
        private float lastAttackTime;
        private bool hasDealtDamageThisAttack = false;

        [Header("Huntress Movement")]
        public bool facingRight = true;
        public float patrolDistance = 4f;   // Phạm vi đi tuần
        private float startX;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;
        private float lastFlipTime;
        private float flipCooldown = 0.5f;

        [Header("Huntress Attack")]
        public Transform attackPoint;
        public float attackRadius = 1.5f;
        public LayerMask playerLayer;
        public GameObject arrowPrefab;     // Prefab Mũi Tên (Arrow)

        [Header("Huntress Audio")]
        public AudioClip attackSound;

        protected override void Start()
        {
            base.Start();
            facingRight = true;
            startX = transform.position.x;
            moveSpeed = 2.8f + Random.Range(-0.2f, 0.3f);
            maxHealth = 140f;
            currentHealth = maxHealth;
            defense = 60f;
            attackRange = Mathf.Max(attackRange, 6.0f);

            if (groundLayer.value == 0)
            {
                groundLayer = LayerMask.GetMask("Ground");
            }
            if (playerLayer.value == 0)
            {
                playerLayer = LayerMask.GetMask("Player");
            }
        }

        void Update()
        {
            if (currentHealth <= 0) return;
            if (!EnsurePlayerReference()) return;

            float xDiff = player.position.x - transform.position.x;
            float absXDiff = Mathf.Abs(xDiff);
            float yDiff = Mathf.Abs(player.position.y - transform.position.y);
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // 1. Tấn công nếu trong tầm bắn và chênh lệch độ cao vừa phải
            if (absXDiff <= attackRange && yDiff <= 3.5f && playerScript.currentHealth > 0)
            {
                AttackPlayer();
            }
            // 2. Đuổi theo nếu trong tầm nhìn
            else if (distanceToPlayer <= chaseRange && yDiff <= 5.0f && playerScript.currentHealth > 0)
            {
                ChasePlayer();
            }
            // 3. Đi tuần
            else
            {
                Patrol();
            }
        }

        private void Flip()
        {
            if (Time.time < lastFlipTime + flipCooldown) return;

            facingRight = !facingRight;
            float absX = Mathf.Abs(transform.localScale.x);
            transform.localScale = new Vector3(facingRight ? absX : -absX, transform.localScale.y, transform.localScale.z);
            lastFlipTime = Time.time;
        }

        private void Patrol()
        {
            if (anim != null) anim.SetBool("isRunning", true);
            float direction = facingRight ? 1f : -1f;
            if (rb != null) rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

            // 1. Tự động quay đầu nếu đi quá phạm vi tuần tra
            if (patrolDistance > 0)
            {
                if (facingRight && transform.position.x >= startX + patrolDistance)
                {
                    Flip();
                    return;
                }
                else if (!facingRight && transform.position.x <= startX - patrolDistance)
                {
                    Flip();
                    return;
                }
            }

            // 2. Kiểm tra vực thẳm hoặc va chạm tường
            if (groundLayer.value == 0) return;

            bool isGrounded = true;
            if (groundCheck != null)
            {
                isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.35f, groundLayer);
            }

            bool isWallHit = false;
            if (wallCheck != null)
            {
                isWallHit = Physics2D.OverlapCircle(wallCheck.position, 0.2f, groundLayer);
            }

            if ((groundCheck != null && !isGrounded) || isWallHit)
            {
                Flip();
            }
        }

        private void ChasePlayer()
        {
            if (anim != null) anim.SetBool("isRunning", true);

            float xDiff = player.position.x - transform.position.x;

            if (xDiff > 0.2f && !facingRight)
            {
                Flip();
            }
            else if (xDiff < -0.2f && facingRight)
            {
                Flip();
            }

            float moveDirection = facingRight ? 1f : -1f;
            if (rb != null) rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        }

        private void AttackPlayer()
        {
            if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);

            float xDiff = player.position.x - transform.position.x;
            if (xDiff > 0.1f && !facingRight)
            {
                Flip();
            }
            else if (xDiff < -0.1f && facingRight)
            {
                Flip();
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                hasDealtDamageThisAttack = false;
                if (anim != null) anim.SetTrigger("attack");
                TriggerAtkSound();
                lastAttackTime = Time.time;
                StartCoroutine(DamageRoutine(0.45f));
            }
        }

        private IEnumerator DamageRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            DealDamageToPlayer();
        }

        // Được gọi trong Animation Event của Huntress hoặc từ DamageRoutine
        public void DealDamageToPlayer()
        {
            if (hasDealtDamageThisAttack) return;
            hasDealtDamageThisAttack = true;

            if (arrowPrefab != null)
            {
                SpawnArrow();
                return;
            }

            if (attackPoint == null) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius > 0.2f ? attackRadius : 1.5f, playerLayer);
            foreach (var col in hits)
            {
                Player p = col.GetComponentInParent<Player>();
                if (p != null && p.currentHealth > 0)
                {
                    p.TakeDamage(attackDamage);
                    break;
                }
            }
        }

        public void SpawnArrow()
        {
            Vector3 spawnPos = (attackPoint != null) ? attackPoint.position : transform.position;

            if (arrowPrefab != null)
            {
                GameObject arrowObj = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
                Arrow arrowScript = arrowObj.GetComponent<Arrow>();
                if (arrowScript != null)
                {
                    arrowScript.Setup(facingRight, attackDamage);
                }
            }
        }

        private void TriggerAtkSound()
        {
            if (audioManager != null && attackSound != null)
            {
                audioManager.PlayerSFX(attackSound);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (attackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
            }

            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, 0.35f);
            }

            if (wallCheck != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(wallCheck.position, 0.15f);
            }
        }
    }
}
