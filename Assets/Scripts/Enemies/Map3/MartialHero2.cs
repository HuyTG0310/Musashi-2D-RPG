using UnityEngine;

namespace Assets.Scripts.Enemies.Map3
{
    public class MartialHero2 : EnemyBase
    {
        [Header("MartialHero2 AI & Combat")]
        public float chaseRange = 7f;        // tầm nhìn phát hiện
        public float attackRange = 1.5f;     // tầm đánh (chung cho cả 2 đòn)
        public float attackCooldown = 1.8f;  // hồi chiêu giữa 2 lần tấn công
        public float lastAttackTime;

        [Header("MartialHero2 Attack 1 - Đòn nhẹ")]
        public float attack1Damage = 15f;
        public float attack1Radius = 0.6f;
        public Transform attackPoint1;

        [Header("MartialHero2 Attack 2 - Đòn mạnh")]
        public float attack2Damage = 35f;
        public float attack2Radius = 0.8f;
        public Transform attackPoint2;

        public LayerMask playerLayer;

        [Header("MartialHero2 Movement")]
        public bool facingRight = true;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;
        public float patrolDistance = 3f;   // đi xa tối đa bao nhiêu (mỗi bên) so với điểm gốc
        private Vector3 startPosition;      // vị trí ban đầu lúc spawn, dùng làm tâm patrol

        [Header("MartialHero2 Sounds")]
        public AudioClip attack1Sound;
        public AudioClip attack2Sound;

        // đang trong combo Attack1 -> Attack2 thì không di chuyển
        private bool isAttacking = false;
        private float lastCheckedHealth = -1f; // phát hiện vừa bị đánh (máu giảm) để tự giải phóng isAttacking

        // chống giật khi patrol (gộp các điều kiện Flip thành 1 quyết định/frame + cooldown)
        private float lastPatrolFlipTime = -999f;
        private const float patrolFlipCooldown = 0.3f;


        protected override void Start()
        {
            base.Start();
            facingRight = true;
            float randomOffset = Random.Range(-0.5f, 0.5f);
            moveSpeed += randomOffset;

            startPosition = transform.position; // lưu vị trí spawn làm tâm patrol
        }


        void FixedUpdate()
        {
            if (currentHealth <= 0 || player == null)
            {
                return;
            }

            // phát hiện vừa bị đánh trúng (máu giảm) trong lúc đang isAttacking ->
            // animation Attack có thể đã bị Hurt ngắt giữa chừng, khiến Event EndAttack()
            // không bao giờ được gọi -> tự giải phóng isAttacking để tránh kẹt vĩnh viễn
            if (lastCheckedHealth < 0f)
            {
                lastCheckedHealth = currentHealth;
            }
            else if (currentHealth < lastCheckedHealth)
            {
                isAttacking = false;
                lastCheckedHealth = currentHealth;
            }
            else
            {
                lastCheckedHealth = currentHealth;
            }

            // Trong toàn bộ combo Attack1 -> Attack2: tuyệt đối không di chuyển theo Player
            if (isAttacking)
            {
                rb.velocity = Vector2.zero;
                anim.SetBool("isRunning", false);
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (playerScript.currentHealth <= 0)
            {
                Patrol();
                return;
            }

            if (distanceToPlayer <= attackRange)
            {
                AttackPlayer();
            }
            else if (distanceToPlayer <= chaseRange)
            {
                ChasePlayer();
            }
            else
            {
                Patrol();
            }
        }

        // method đổi hướng di chuyển
        private void Flip()
        {
            facingRight = !facingRight;
            // giữ nguyên Y và Z hiện tại, chỉ đảo dấu trục X -
            // tránh ghi đè Scale Y/Z đã chỉnh tay trong Inspector về lại giá trị mặc định 1
            Vector3 currentScale = transform.localScale;
            transform.localScale = new Vector3(-currentScale.x, currentScale.y, currentScale.z);
        }

        // method đi tuần tra trong 1 khoảng cách cố định quanh điểm spawn
        private void Patrol()
        {
            anim.SetBool("isRunning", true);

            // di chuyển theo hướng hiện tại
            float direction = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

            // gộp CẢ 3 điều kiện vào 1 quyết định Flip DUY NHẤT mỗi frame (chống double-flip gây giật)
            bool reachedPatrolLimit =
                (transform.position.x >= startPosition.x + patrolDistance && facingRight) ||
                (transform.position.x <= startPosition.x - patrolDistance && !facingRight);

            bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.25f, groundLayer);

            // dò tường chắn ngang - groundCheck chỉ bắt được hố/vực, không bắt được tường thẳng đứng
            bool hitWall = wallCheck != null && Physics2D.OverlapCircle(wallCheck.position, 0.2f, groundLayer);

            // cooldown chống giật: không cho Flip quá nhanh liên tiếp dù điều kiện dao động sát ranh giới
            if ((reachedPatrolLimit || !isGrounded || hitWall) &&
                Time.time >= lastPatrolFlipTime + patrolFlipCooldown)
            {
                Flip();
                lastPatrolFlipTime = Time.time;
            }
        }


        // method đuổi theo player
        private void ChasePlayer()
        {
            anim.SetBool("isRunning", true);

            // khoảng đệm để tránh Flip liên tục khi đứng gần sát player
            float flipDeadzone = 0.15f;
            float xDifference = player.position.x - transform.position.x;

            if (xDifference > flipDeadzone && !facingRight)
            {
                Flip();
            }
            else if (xDifference < -flipDeadzone && facingRight)
            {
                Flip();
            }

            float moveDirection = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        }

        // hàm kích hoạt animation tấn công player
        private void AttackPlayer()
        {
            rb.velocity = Vector2.zero;
            anim.SetBool("isRunning", false);

            // Quay mặt về Player
            float xDifference = player.position.x - transform.position.x;

            if (xDifference > 0 && !facingRight)
            {
                Flip();
            }
            else if (xDifference < 0 && facingRight)
            {
                Flip();
            }

            // Chỉ bắt đầu trạng thái attack khi thực sự trigger animation
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                isAttacking = true;

                // chỉ bắn trigger attack1, Animator tự nối tiếp attack1 -> attack2
                anim.SetTrigger("attack1");

                lastAttackTime = Time.time;
            }
        }


        // được gọi trong animation Attack1 (đòn nhẹ) của MartialHero2
        public void DealDamageAttack1()
        {
            // Phát âm thanh đòn 1 đúng frame ra đòn
            TriggerAttack1Sound();

            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint1.position, attack1Radius, playerLayer);

            if (hitPlayer != null)
            {
                Player playerHit = hitPlayer.GetComponentInParent<Player>();
                if (playerHit != null)
                {
                    playerHit.TakeDamage(attack1Damage);
                }
            }
        }

        // được gọi trong animation Attack2 (đòn mạnh) của MartialHero2
        public void DealDamageAttack2()
        {
            // Phát âm thanh đòn 2 đúng frame ra đòn
            TriggerAttack2Sound();

            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint2.position, attack2Radius, playerLayer);

            if (hitPlayer != null)
            {
                Player playerHit = hitPlayer.GetComponentInParent<Player>();
                if (playerHit != null)
                {
                    playerHit.TakeDamage(attack2Damage);
                }
            }
        }

        // method vẽ vòng tròn hitbox để dễ quan sát cả 2 đòn
        private void OnDrawGizmosSelected()
        {
            if (attackPoint1 != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(attackPoint1.position, attack1Radius);
            }

            if (attackPoint2 != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint2.position, attack2Radius);
            }
        }


        public void TriggerAttack1Sound()
        {
            if (audioManager == null)
            {
                audioManager = FindAnyObjectByType<AudioManager>();
            }
            if (audioManager != null && attack1Sound != null)
            {
                audioManager.PlayerSFX(attack1Sound);
            }
        }

        public void TriggerAttack2Sound()
        {
            if (audioManager == null)
            {
                audioManager = FindAnyObjectByType<AudioManager>();
            }
            if (audioManager != null && attack2Sound != null)
            {
                audioManager.PlayerSFX(attack2Sound);
            }
        }

        // Gọi bằng Animation Event ở frame cuối clip Atk2 (kết thúc combo)
        public void EndAttack()
        {
            isAttacking = false;
        }
    }
}