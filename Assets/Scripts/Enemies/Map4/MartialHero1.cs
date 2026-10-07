using UnityEngine;

namespace Assets.Scripts.Enemies.Map4
{
    public class MartialHero1 : EnemyBase
    {
        [Header("MartialHero1 AI & Combat")]
        public float chaseRange = 7f;        // Tầm nhìn phát hiện người chơi
        public float attackRange = 1.6f;     // Tầm đánh cận chiến
        public float attackCooldown = 1.4f;  // Thời gian hồi chiêu
        public float attackDamage = 35f;     // Sát thương đòn đánh
        public float lastAttackTime;

        [Header("MartialHero1 Movement")]
        public bool facingRight = true;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;

        [Header("MartialHero1 Attack")]
        public Transform attackPoint;
        public float attackRadius = 0.8f;
        public LayerMask playerLayer;

        [Header("MartialHero1 Sounds")]
        public AudioClip attackSound;

        protected override void Start()
        {
            base.Start();
            facingRight = true;
            float randomOffset = Random.Range(-0.3f, 0.3f);
            moveSpeed += randomOffset;
        }

        void Update()
        {
            if (currentHealth <= 0 || player == null || playerScript == null || anim == null)
            {
                return;
            }

            // Khi đang bị đánh (Hurt), dừng di chuyển tạm thời
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Hurt") || stateInfo.IsName("Take hit") || stateInfo.IsName("MartialHero1_Hurt"))
            {
                rb.velocity = Vector2.zero;
                return;
            }

            // Tính khoảng cách đến Player
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // Nếu trong tầm đánh và player còn sống
            if (distanceToPlayer <= attackRange && playerScript.currentHealth > 0)
            {
                AttackPlayer();
            }
            // Nếu trong tầm nhìn đuổi theo
            else if (distanceToPlayer <= chaseRange && playerScript.currentHealth > 0)
            {
                ChasePlayer();
            }
            // Còn lại thì đi tuần tra
            else
            {
                Patrol();
            }
        }

        // Đổi hướng nhân vật
        private void Flip()
        {
            facingRight = !facingRight;
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        }

        // Tuần tra khu vực
        private void Patrol()
        {
            anim.SetBool("isRunning", true);

            float direction = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

            bool isGrounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
            bool isWallHit = wallCheck != null && Physics2D.OverlapCircle(wallCheck.position, 0.2f, groundLayer);

            // Đổi hướng nếu gặp mép vực hoặc chạm tường
            if (!isGrounded || isWallHit)
            {
                Flip();
            }
        }

        // Đuổi theo Player
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

        // Kích hoạt animation tấn công
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

        // Gọi từ Animation Event trong animation Attack
        public void DealDamageToPlayer()
        {
            if (attackPoint == null) return;

            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
            if (hitPlayer != null)
            {
                Player target = hitPlayer.GetComponent<Player>();
                if (target != null)
                {
                    target.TakeDamage(attackDamage);
                }
            }
        }

        // Gọi từ Animation Event trong animation Attack để phát âm thanh
        public void TriggerAtkHandSound()
        {
            TriggerAttackSound();
        }

        public void TriggerAttackSound()
        {
            if (audioManager != null && attackSound != null)
            {
                audioManager.PlayerSFX(attackSound);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}
