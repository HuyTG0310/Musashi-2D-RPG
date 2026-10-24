using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Enemies.Map2
{
    public class Skeleton : EnemyBase
    {
        [Header("Skeleton AI & Combat")]
        public float chaseRange = 7f;       // Tầm nhìn rộng hơn
        public float attackRange = 2.5f;    // Tầm đánh cận chiến (đủ lớn hơn khoảng cách va chạm vật lý 1.85f)
        public float attackCooldown = 1.8f;   // Thời gian hồi chiêu
        public float attackDamage = 30f;    // Sát thương cao hơn (30 HP)
        private float lastAttackTime;
        private bool hasDealtDamageThisAttack = false;

        [Header("Skeleton Movement")]
        public bool facingRight = true;
        public float patrolDistance = 4f;   // Khoảng cách tuần tra tối đa từ điểm ban đầu
        private float startX;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;
        private float lastFlipTime;
        private float flipCooldown = 0.5f;

        [Header("Skeleton Attack")]
        public Transform attackPoint;
        public float attackRadius = 1.8f;   // Bán kính đòn chém
        public LayerMask playerLayer;

        [Header("Skeleton Audio")]
        public AudioClip attackSound;

        protected override void Start()
        {
            base.Start();
            facingRight = true;
            startX = transform.position.x;
            moveSpeed = 2.2f + Random.Range(-0.2f, 0.3f);
            maxHealth = 180f;
            currentHealth = maxHealth;
            defense = 80f;
            attackRange = Mathf.Max(attackRange, 2.5f);

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

            // 1. Tấn công nếu trong tầm đánh ngang và cùng sàn
            if (absXDiff <= attackRange && yDiff <= 2.2f && playerScript.currentHealth > 0)
            {
                AttackPlayer();
            }
            // 2. Đuổi theo nếu trong tầm nhìn
            else if (distanceToPlayer <= chaseRange && yDiff <= 4.5f && playerScript.currentHealth > 0)
            {
                ChasePlayer();
            }
            // 3. Tuần tra bình thường
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
                StartCoroutine(DamageRoutine(0.4f));
            }
        }

        private IEnumerator DamageRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            DealDamageToPlayer();
        }

        // Được gọi trong Animation Event của Skeleton hoặc từ DamageRoutine
        public void DealDamageToPlayer()
        {
            if (hasDealtDamageThisAttack) return;
            hasDealtDamageThisAttack = true;

            bool hit = false;
            if (attackPoint != null)
            {
                float rad = attackRadius > 0.2f ? attackRadius : 1.8f;
                Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, rad, playerLayer);
                foreach (var col in hitPlayers)
                {
                    Player p = col.GetComponentInParent<Player>();
                    if (p != null && p.currentHealth > 0)
                    {
                        p.TakeDamage(attackDamage);
                        hit = true;
                        break;
                    }
                }
            }

            // Dự phòng nếu OverlapCircle không chạm do lệch trục
            if (!hit && playerScript != null && playerScript.currentHealth > 0)
            {
                float absX = Mathf.Abs(player.position.x - transform.position.x);
                float yDiff = Mathf.Abs(player.position.y - transform.position.y);
                if (absX <= attackRange + 0.6f && yDiff <= 2.2f)
                {
                    playerScript.TakeDamage(attackDamage);
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
                Gizmos.color = Color.magenta;
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
