using UnityEngine;

namespace Assets.Scripts.Enemies.Map2
{
    public class Goblin : EnemyBase
    {
        [Header("Goblin AI & Combat")]
        public float chaseRange = 6f;       // Tầm nhìn phát hiện người chơi
        public float attackRange = 1.3f;    // Tầm đánh cận chiến
        public float attackCooldown = 1.5f; // Thời gian hồi chiêu
        public float attackDamage = 25f;    // Sát thương đòn đánh
        private float lastAttackTime;

        [Header("Goblin Movement")]
        public bool facingRight = true;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;
        private float lastFlipTime;
        private float flipCooldown = 0.5f; // Giới hạn tần suất đổi hướng

        [Header("Goblin Attack")]
        public Transform attackPoint;
        public float attackRadius = 0.6f;
        public LayerMask playerLayer;

        [Header("Goblin Audio")]
        public AudioClip attackSound;

        protected override void Start()
        {
            base.Start();
            facingRight = true;
            moveSpeed = 3f + Random.Range(-0.3f, 0.5f);
            maxHealth = 120f;
            currentHealth = maxHealth;
            defense = 50f;
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

            // Kiểm tra xem groundLayer đã được chọn chưa (khác Nothing)
            if (groundLayer.value == 0) return;

            bool isGrounded = true;
            if (groundCheck != null)
            {
                isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.25f, groundLayer);
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

            if (player.position.x > transform.position.x && !facingRight)
            {
                Flip();
            }
            else if (player.position.x < transform.position.x && facingRight)
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

        // Được gọi trong Animation Event của Animation Attack của Goblin
        public void DealDamageToPlayer()
        {
            if (attackPoint == null) return;
            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);

            if (hitPlayer != null)
            {
                Player p = hitPlayer.GetComponent<Player>();
                if (p != null)
                {
                    p.TakeDamage(attackDamage);
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
            if (attackPoint == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}
