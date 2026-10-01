using UnityEngine;

namespace Assets.Scripts.Enemies.Map2
{
    public class Huntress2 : EnemyBase
    {
        [Header("Huntress AI & Combat")]
        public float chaseRange = 8f;       // Tầm nhìn phát hiện Musashi
        public float attackRange = 2.5f;    // Tầm đánh cận chiến (tăng lên 2.5f để nhận diện đòn đánh dễ dàng)
        public float attackCooldown = 1.6f; // Thời gian hồi chiêu
        public float attackDamage = 28f;    // Sát thương đòn đánh
        private float lastAttackTime;

        [Header("Huntress Movement")]
        public bool facingRight = true;
        public float patrolDistance = 4f;   // Phạm vi đi tuần (đặt 0 nếu muốn đi đụng tường mới quay đầu)
        private float startX;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;
        private float lastFlipTime;
        private float flipCooldown = 0.5f;

        [Header("Huntress Attack")]
        public Transform attackPoint;
        public float attackRadius = 1.2f;   // Bán kính đòn chém (tăng lên 1.2f)
        public LayerMask playerLayer;
        public GameObject arrowPrefab;     // Prefab Mũi Tên (Arrow)

        [Header("Huntress Audio")]
        public AudioClip attackSound;

        protected override void Start()
        {
            base.Start();
            facingRight = true;
            startX = transform.position.x;
            moveSpeed = 2.8f + Random.Range(-0.2f, 0.4f);
            maxHealth = 140f;
            currentHealth = maxHealth;
            defense = 60f;
        }

        void Update()
        {
            if (currentHealth <= 0 || player == null || playerScript == null) return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= attackRange && playerScript.currentHealth > 0)
            {
                AttackPlayer();
            }
            else if (distanceToPlayer <= chaseRange && playerScript.currentHealth > 0)
            {
                ChasePlayer();
            }
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

            // 1. Tự động quay đầu nếu đi quá phạm vi tuần tra (chỉ áp dụng nếu patrolDistance > 0)
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

            // 2. Kiểm tra vực thẳm hoặc va chạm tường (nếu có gán groundLayer)
            if (groundLayer.value == 0) return;

            bool isGrounded = true;
            if (groundCheck != null)
            {
                isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.35f, groundLayer);
            }

            bool isWallHit = false;
            if (wallCheck != null)
            {
                isWallHit = Physics2D.OverlapCircle(wallCheck.position, 0.15f, groundLayer);
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

            // Chỉ quay đầu khi khoảng cách X đủ lớn (> 0.3f) để tránh bị giật xoay tại chỗ
            if (xDiff > 0.3f && !facingRight)
            {
                Flip();
            }
            else if (xDiff < -0.3f && facingRight)
            {
                Flip();
            }

            float moveDirection = facingRight ? 1f : -1f;
            if (rb != null) rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        }

        private void AttackPlayer()
        {
            if (rb != null) rb.velocity = Vector2.zero;
            if (anim != null) anim.SetBool("isRunning", false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                if (anim != null) anim.SetTrigger("attack");
                lastAttackTime = Time.time;
            }
        }

        // Được gọi trong Animation Event của Animation Attack của Huntress
        public void DealDamageToPlayer()
        {
            if (arrowPrefab != null)
            {
                SpawnArrow();
                return;
            }

            if (attackPoint == null)
            {
                Debug.LogWarning("Huntress: AttackPoint is missing on Inspector!");
                return;
            }

            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);

            if (hitPlayer != null)
            {
                Player p = hitPlayer.GetComponent<Player>();
                if (p != null)
                {
                    p.TakeDamage(attackDamage);
                    Debug.Log("Huntress hit Musashi for " + attackDamage + " damage!");
                }
            }
        }

        public void SpawnArrow()
        {
            if (attackPoint == null) return;

            if (arrowPrefab != null)
            {
                GameObject arrowObj = Instantiate(arrowPrefab, attackPoint.position, Quaternion.identity);
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
