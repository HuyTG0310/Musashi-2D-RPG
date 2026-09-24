using UnityEngine;

namespace Assets.Scripts.Enemies.Map1
{
    public class HeroKnight2 : EnemyBase
    {
        [Header("HeroKnight2 AI & Combat")]
        public float chaseRange = 7f;       // Tầm nhìn phát hiện Player
        public float attackRange = 1.5f;    // Tầm đánh
        public float attackCooldown = 1.5f; // Thời gian hồi chiêu
        public float attackDamage = 30f;     // Sát thương
        public float lastAttackTime;

        [Header("HeroKnight2 Movement")]
        public bool facingRight = true;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;
        public float patrolDistance = 3f;   // Khoảng cách patrol mỗi bên
        private Vector3 startPosition;       // Vị trí spawn ban đầu

        [Header("HeroKnight2 Attack")]
        public Transform attackPoint;
        public float attackRadius = 0.7f;
        public LayerMask playerLayer;

        // Kiểm tra HeroKnight2 có đang thực hiện animation Attack hay không
        private bool isAttacking = false;

        [Header("HeroKnight2 Sounds")]
        public AudioClip attackSound;


        // =========================================================
        // START
        // =========================================================

        protected override void Start()
        {
            base.Start();

            // Mặc định HeroKnight2 quay mặt sang phải
            facingRight = true;

            // Random tốc độ một chút để các enemy không giống nhau hoàn toàn
            float randomOffset = Random.Range(-0.5f, 0.5f);
            moveSpeed += randomOffset;

            // Lưu vị trí spawn làm tâm patrol
            startPosition = transform.position;
        }


        // =========================================================
        // FIXED UPDATE
        // =========================================================

        void FixedUpdate()
        {
            // Nếu HeroKnight2 chết hoặc không tìm thấy Player
            // thì không làm gì
            if (currentHealth <= 0 || player == null)
            {
                return;
            }


            // =====================================================
            // ĐANG ATTACK
            // =====================================================

            // Nếu đang thực hiện animation Attack
            // thì tuyệt đối không được di chuyển
            if (isAttacking)
            {
                rb.velocity = Vector2.zero;
                return;
            }


            // =====================================================
            // TÍNH KHOẢNG CÁCH PLAYER
            // =====================================================

            float distanceToPlayer =
                Vector2.Distance(transform.position, player.position);


            // =====================================================
            // ATTACK
            // =====================================================

            // Nếu Player nằm trong tầm đánh
            if (distanceToPlayer <= attackRange &&
                playerScript.currentHealth > 0)
            {
                AttackPlayer();
            }


            // =====================================================
            // CHASE
            // =====================================================

            // Nếu Player nằm trong tầm phát hiện
            else if (distanceToPlayer <= chaseRange &&
                     playerScript.currentHealth > 0)
            {
                ChasePlayer();
            }


            // =====================================================
            // PATROL
            // =====================================================

            // Nếu Player ở quá xa thì đi tuần
            else
            {
                Patrol();
            }
        }


        // =========================================================
        // FLIP
        // =========================================================

        private void Flip()
        {
            // Đảo hướng
            facingRight = !facingRight;

            // Lật Sprite
            transform.localScale =
                new Vector3(-transform.localScale.x, 1, 1);
        }


        // =========================================================
        // PATROL
        // =========================================================

        private void Patrol()
        {
            // Bật animation chạy
            anim.SetBool("isRunning", true);

            // Xác định hướng di chuyển
            float direction = facingRight ? 1f : -1f;

            // Di chuyển
            rb.velocity =
                new Vector2(direction * moveSpeed, rb.velocity.y);


            // =====================================================
            // KIỂM TRA GIỚI HẠN PATROL BÊN PHẢI
            // =====================================================

            if (transform.position.x >=
                startPosition.x + patrolDistance &&
                facingRight)
            {
                Flip();
            }


            // =====================================================
            // KIỂM TRA GIỚI HẠN PATROL BÊN TRÁI
            // =====================================================

            else if (transform.position.x <=
                     startPosition.x - patrolDistance &&
                     !facingRight)
            {
                Flip();
            }


            // =====================================================
            // KIỂM TRA MẶT ĐẤT
            // =====================================================

            bool isGrounded =
                Physics2D.OverlapCircle(
                    groundCheck.position,
                    0.2f,
                    groundLayer
                );


            // Nếu không còn đứng trên mặt đất
            // thì quay đầu
            if (!isGrounded)
            {
                Flip();
            }
        }


        // =========================================================
        // CHASE PLAYER
        // =========================================================

        private void ChasePlayer()
        {
            // Bật animation chạy
            anim.SetBool("isRunning", true);


            // Khoảng chết để tránh Flip liên tục
            float flipDeadzone = 0.15f;


            // Tính khoảng cách theo trục X
            float xDifference =
                player.position.x - transform.position.x;


            // =====================================================
            // PLAYER Ở BÊN PHẢI
            // =====================================================

            if (xDifference > flipDeadzone &&
                !facingRight)
            {
                Flip();
            }


            // =====================================================
            // PLAYER Ở BÊN TRÁI
            // =====================================================

            else if (xDifference < -flipDeadzone &&
                     facingRight)
            {
                Flip();
            }


            // =====================================================
            // DI CHUYỂN
            // =====================================================

            float moveDirection =
                facingRight ? 1f : -1f;

            rb.velocity =
                new Vector2(
                    moveDirection * moveSpeed,
                    rb.velocity.y
                );
        }


        // =========================================================
        // ATTACK PLAYER
        // =========================================================

        private void AttackPlayer()
        {
            // =====================================================
            // ĐỨNG YÊN
            // =====================================================

            // Khi attack thì không được di chuyển
            rb.velocity = Vector2.zero;


            // Tắt animation chạy
            anim.SetBool("isRunning", false);


            // =====================================================
            // KIỂM TRA CÓ ĐANG ATTACK KHÔNG
            // =====================================================

            // Nếu đang attack thì không trigger Attack lần nữa
            if (isAttacking)
            {
                return;
            }


            // =====================================================
            // KIỂM TRA COOLDOWN
            // =====================================================

            if (Time.time >=
                lastAttackTime + attackCooldown)
            {
                // Đánh dấu đang attack
                isAttacking = true;


                // Trigger animation Attack
                anim.SetTrigger("attack");


                // Lưu thời gian attack
                lastAttackTime = Time.time;
            }
        }


        // =========================================================
        // DEAL DAMAGE
        // =========================================================

        // Hàm này được gọi bằng Animation Event
        // tại frame HeroKnight2 vung kiếm trúng Player
        public void DealDamageToPlayer()
        {
            // Kiểm tra Player trong vùng đánh
            Collider2D hitPlayer = Physics2D.OverlapCircle(
                attackPoint.position,
                attackRadius,
                playerLayer
            );

            // Nếu tìm thấy Player
            if (hitPlayer != null)
            {
                // Lấy Player component
                Player playerScript = hitPlayer.GetComponentInParent<Player>();

                // Nếu tìm thấy Player thì gây damage
                if (playerScript != null)
                {
                    playerScript.TakeDamage(attackDamage);
                }
            }
        }


        // =========================================================
        // END ATTACK
        // =========================================================

        // Hàm này được gọi bằng Animation Event
        // ở frame cuối của Attack animation
        public void EndAttack()
        {
            // Cho phép HeroKnight2 di chuyển lại
            isAttacking = false;
        }


        // =========================================================
        // ATTACK SOUND
        // =========================================================

        // Hàm này được gọi bằng Animation Event
        // tại frame phát âm thanh đánh
        private void TriggerAtkHandSound()
        {
            if (audioManager != null)
            {
                audioManager.PlayerSFX(attackSound);
            }
        }


        // =========================================================
        // GIZMOS
        // =========================================================

        // Hiển thị Attack Hitbox trong Scene
        private void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}