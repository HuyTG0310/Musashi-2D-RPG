using UnityEngine;

namespace Assets.Scripts.Enemies.Map4
{
    public class HeroKnight1 : EnemyBase
    {
        [Header("HeroKnight1 AI & Combat")]
        public float chaseRange = 7f;       // Tầm nhìn phát hiện người chơi (đồng bộ với HeroKnight2)
        public float attackRange = 1.5f;    // Tầm đánh cận chiến (đồng bộ với HeroKnight2)
        public float attackCooldown = 1.5f; // Thời gian hồi chiêu
        public float attackDamage = 30f;    // Sát thương đòn đánh (đồng bộ với HeroKnight2)
        public float lastAttackTime;

        [Header("HeroKnight1 Movement")]
        public bool facingRight = true;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;
        public float patrolDistance = 3f;   // Khoảng cách tuần tra (đồng bộ với HeroKnight2)
        private Vector3 startPosition;      // Vị trí spawn ban đầu

        [Header("HeroKnight1 Attack")]
        public Transform attackPoint;
        public float attackRadius = 1.5f;   // Bán kính vùng đánh (đồng bộ với HeroKnight2)
        public LayerMask playerLayer;

        [Header("HeroKnight1 Sounds")]
        public AudioClip attackSound;

        protected override void Start()
        {
            base.Start();
            facingRight = true;
            float randomOffset = Random.Range(-0.3f, 0.3f);
            moveSpeed += randomOffset;
            startPosition = transform.position;
        }

        void Update()
        {
            if (currentHealth <= 0 || player == null || playerScript == null || anim == null)
            {
                return;
            }

            // Khi đang bị đánh (Hurt), dừng di chuyển tạm thời
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Hurt") || stateInfo.IsName("Take hit") || stateInfo.IsName("HeroKnight1_Hurt"))
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
            Vector3 currentScale = transform.localScale;
            transform.localScale = new Vector3(-currentScale.x, currentScale.y, currentScale.z);
        }

        // Tuần tra khu vực
        private void Patrol()
        {
            anim.SetBool("isRunning", true);

            float direction = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

            bool reachedLimit = (transform.position.x >= startPosition.x + patrolDistance && facingRight) ||
                                (transform.position.x <= startPosition.x - patrolDistance && !facingRight);

            bool isGrounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
            bool isWallHit = wallCheck != null && Physics2D.OverlapCircle(wallCheck.position, 0.2f, groundLayer);

            // Đổi hướng nếu gặp mép vực, chạm tường hoặc hết cự ly tuần tra
            if (reachedLimit || !isGrounded || isWallHit)
            {
                Flip();
            }
        }

        // Đuổi theo Player
        private void ChasePlayer()
        {
            anim.SetBool("isRunning", true);

            float flipDeadzone = 0.15f;
            float xDiff = player.position.x - transform.position.x;

            if (xDiff > flipDeadzone && !facingRight)
            {
                Flip();
            }
            else if (xDiff < -flipDeadzone && facingRight)
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
                Player target = hitPlayer.GetComponentInParent<Player>();
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

