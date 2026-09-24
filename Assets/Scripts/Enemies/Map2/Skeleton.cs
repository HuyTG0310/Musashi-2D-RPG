using UnityEngine;

namespace Assets.Scripts.Enemies.Map2
{
    public class Skeleton : EnemyBase
    {
        [Header("Skeleton AI & Combat")]
        public float chaseRange = 7f;       // Tầm nhìn rộng hơn
        public float attackRange = 1.4f;    // Tầm đánh rộng hơn
        public float attackCooldown = 2f;   // Thời gian hồi chiêu
        public float attackDamage = 30f;    // Sát thương cao hơn
        private float lastAttackTime;

        [Header("Skeleton Movement")]
        public bool facingRight = true;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;

        [Header("Skeleton Attack")]
        public Transform attackPoint;
        public float attackRadius = 0.7f;
        public LayerMask playerLayer;

        [Header("Skeleton Audio")]
        public AudioClip attackSound;

        protected override void Start()
        {
            base.Start();
            facingRight = true;
            moveSpeed = 2.2f + Random.Range(-0.2f, 0.3f);
            maxHealth = 180f;
            currentHealth = maxHealth;
            defense = 80f;
        }

        void Update()
        {
            if (currentHealth <= 0 || player == null) return;

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
            facingRight = !facingRight;
            transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
        }

        private void Patrol()
        {
            anim.SetBool("isRunning", true);
            float direction = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

            bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
            bool isWallHit = Physics2D.OverlapCircle(wallCheck.position, 0.2f, groundLayer);

            if (!isGrounded || isWallHit)
            {
                Flip();
            }
        }

        private void ChasePlayer()
        {
            anim.SetBool("isRunning", true);

            if (player.position.x > transform.position.x && !facingRight)
            {
                Flip();
            }
            else if (player.position.x < transform.position.x && facingRight)
            {
                Flip();
            }

            float moveDirection = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        }

        private void AttackPlayer()
        {
            rb.velocity = Vector2.zero;
            anim.SetBool("isRunning", false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                anim.SetTrigger("attack");
                lastAttackTime = Time.time;
            }
        }

        // Được gọi trong Animation Event của Animation Attack của Skeleton
        public void DealDamageToPlayer()
        {
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
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}
