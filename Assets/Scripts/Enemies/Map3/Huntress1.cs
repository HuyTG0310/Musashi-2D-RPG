using UnityEngine;

namespace Assets.Scripts.Enemies.Map3
{
    // Huntress1:
    // - Detect Player
    // - Chase Player until spear range
    // - Stop and throw spear
    // - Stay in spear range while spear is on cooldown
    // - Switch to melee when Player gets close
    // - Melee combo: attack1 -> attack2
    public class Huntress1 : EnemyBase
    {
        [Header("Huntress1 Ranges")]
        public float detectionRange = 12f; // Tầm phát hiện Player
        public float spearRange = 10f;      // Tầm bắt đầu ném spear
        public float meleeRange = 1.5f;    // Tầm đánh cận chiến

        // Đang thực hiện melee combo hoặc throw
        private bool isBusy = false;


        [Header("Huntress1 Movement")]
        public bool facingRight = true;
        public Transform groundCheck;
        public LayerMask groundLayer;
        public float patrolDistance = 3f;

        // Vị trí spawn ban đầu
        private Vector3 startPosition;


        [Header("Huntress1 Attack Point")]
        public Transform attackPoint;


        [Header("Huntress1 Melee")]
        public float meleeRadius = 0.6f;

        // Damage đòn 1
        public float meleeDamageHit1 = 10f;

        // Damage đòn 2
        public float meleeDamageHit2 = 15f;

        // Cooldown giữa các combo
        public float meleeCooldown = 2f;

        // Thời điểm melee gần nhất
        public float lastMeleeTime;

        // Layer của Player
        public LayerMask playerLayer;


        [Header("Huntress1 Sounds")]
        public AudioClip meleeSound;


        // Component xử lý việc ném spear
        private SpearThrower spearThrower;


        // =========================================================
        // START
        // =========================================================

        protected override void Start()
        {
            base.Start();

            // Mặc định quay sang phải
            facingRight = true;

            // Random tốc độ một chút
            float randomOffset =
                Random.Range(-0.5f, 0.5f);

            moveSpeed += randomOffset;

            // Lưu vị trí spawn
            startPosition = transform.position;

            // Lấy component SpearThrower
            spearThrower =
                GetComponent<SpearThrower>();
        }


        // =========================================================
        // FIXED UPDATE
        // =========================================================

        void FixedUpdate()
        {
            // Không có Player
            if (player == null || playerScript == null)
                return;


            // =====================================================
            // ĐANG ATTACK / THROW
            // =====================================================

            // Không được di chuyển khi đang thực hiện animation
            if (isBusy)
            {
                rb.velocity = Vector2.zero;
                return;
            }


            // =====================================================
            // TÍNH KHOẢNG CÁCH
            // =====================================================

            float distanceToPlayer =
                Vector2.Distance(
                    transform.position,
                    player.position
                );


            // =====================================================
            // PLAYER CHẾT
            // =====================================================

            if (playerScript.currentHealth <= 0)
            {
                Patrol();
                return;
            }


            // =====================================================
            // 1. MELEE RANGE
            // =====================================================

            // Player ở rất gần
            // => ưu tiên melee
            if (distanceToPlayer <= meleeRange)
            {
                MeleeAttack();
                return;
            }


            // =====================================================
            // 2. SPEAR RANGE
            // =====================================================

            // Player nằm trong tầm ném
            //
            // Huntress sẽ:
            // - Đứng yên
            // - Quay mặt về Player
            // - Ném khi spear sẵn sàng
            // - Không chạy khi spear đang cooldown
            else if (distanceToPlayer <= spearRange)
            {
                // Đứng yên
                rb.velocity = Vector2.zero;

                // Tắt animation chạy
                anim.SetBool("isRunning", false);

                // Quay mặt về Player
                FaceTowardsPlayer();


                // Nếu có SpearThrower
                if (spearThrower != null)
                {
                    // Chỉ ném khi spear sẵn sàng
                    if (spearThrower.CanThrow(distanceToPlayer))
                    {
                        ThrowAttack();
                    }
                }

                return;
            }


            // =====================================================
            // 3. DETECTION / CHASE RANGE
            // =====================================================

            // Player đã bị phát hiện
            // nhưng vẫn còn quá xa để ném spear
            else if (distanceToPlayer <= detectionRange)
            {
                ChasePlayer();
                return;
            }


            // =====================================================
            // 4. PATROL
            // =====================================================

            // Player ngoài tầm phát hiện
            Patrol();
        }


        // =========================================================
        // FLIP
        // =========================================================

        private void Flip()
        {
            // Đảo hướng
            facingRight = !facingRight;

            // Giữ nguyên scale Y và Z
            transform.localScale =
                new Vector3(
                    -transform.localScale.x,
                    transform.localScale.y,
                    transform.localScale.z
                );
        }


        // =========================================================
        // FACE TOWARDS PLAYER
        // =========================================================

        private void FaceTowardsPlayer()
        {
            if (player == null)
                return;


            // Tính khoảng cách theo trục X
            float xDifference =
                player.position.x -
                transform.position.x;


            // Player ở bên phải
            if (xDifference > 0 && !facingRight)
            {
                Flip();
            }


            // Player ở bên trái
            else if (xDifference < 0 && facingRight)
            {
                Flip();
            }
        }


        // =========================================================
        // PATROL
        // =========================================================

        private void Patrol()
        {
            // Bật animation chạy
            anim.SetBool("isRunning", true);


            // Xác định hướng
            float direction =
                facingRight ? 1f : -1f;


            // Di chuyển
            rb.velocity =
                new Vector2(
                    direction * moveSpeed,
                    rb.velocity.y
                );


            // =====================================================
            // PATROL RIGHT LIMIT
            // =====================================================

            if (transform.position.x >=
                startPosition.x + patrolDistance &&
                facingRight)
            {
                Flip();
            }


            // =====================================================
            // PATROL LEFT LIMIT
            // =====================================================

            else if (transform.position.x <=
                     startPosition.x - patrolDistance &&
                     !facingRight)
            {
                Flip();
            }


            // =====================================================
            // GROUND CHECK
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


            // Khoảng cách theo trục X
            float xDifference =
                player.position.x -
                transform.position.x;


            // =====================================================
            // PLAYER BÊN PHẢI
            // =====================================================

            if (xDifference > flipDeadzone &&
                !facingRight)
            {
                Flip();
            }


            // =====================================================
            // PLAYER BÊN TRÁI
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
        // MELEE COMBO
        // =========================================================

        private void MeleeAttack()
        {
            // Đứng yên
            rb.velocity = Vector2.zero;


            // Tắt animation chạy
            anim.SetBool("isRunning", false);


            // Quay mặt về Player
            FaceTowardsPlayer();


            // Nếu đang attack thì không attack tiếp
            if (isBusy)
                return;


            // Kiểm tra cooldown
            if (Time.time >=
                lastMeleeTime + meleeCooldown)
            {
                // Đánh dấu đang bận
                isBusy = true;


                // Bắt đầu combo
                anim.SetTrigger("attack1");


                // Lưu thời điểm attack
                lastMeleeTime = Time.time;
            }
        }


        // =========================================================
        // MELEE DAMAGE 1
        // =========================================================

        // Animation Event trong attack1
        public void DealMeleeDamage1()
        {
            // Chỉ gây damage khi đang thực sự attack
            if (!isBusy)
                return;


            DealMeleeDamage(
                meleeDamageHit1
            );
        }


        // =========================================================
        // MELEE DAMAGE 2
        // =========================================================

        // Animation Event trong attack2
        public void DealMeleeDamage2()
        {
            // Chỉ gây damage khi đang thực sự attack
            if (!isBusy)
                return;


            DealMeleeDamage(
                meleeDamageHit2
            );
        }


        // =========================================================
        // DEAL MELEE DAMAGE
        // =========================================================

        private void DealMeleeDamage(
            float damageAmount
        )
        {
            // Kiểm tra AttackPoint
            if (attackPoint == null)
            {
                Debug.LogWarning(
                    "Huntress1: AttackPoint chưa được gán!"
                );

                return;
            }


            // Tìm Player trong hitbox
            Collider2D hitPlayer =
                Physics2D.OverlapCircle(
                    attackPoint.position,
                    meleeRadius,
                    playerLayer
                );


            // Không tìm thấy Player
            if (hitPlayer == null)
                return;


            // Lấy Player component
            Player hitPlayerScript =
                hitPlayer.GetComponentInParent<Player>();


            // Kiểm tra Player
            if (hitPlayerScript != null &&
                hitPlayerScript.currentHealth > 0)
            {
                hitPlayerScript.TakeDamage(
                    damageAmount
                );
            }
        }


        // =========================================================
        // MELEE SOUND
        // =========================================================

        private void TriggerMeleeSound()
        {
            if (audioManager != null)
            {
                audioManager.PlayerSFX(
                    meleeSound
                );
            }
        }


        // =========================================================
        // THROW SPEAR
        // =========================================================

        private void ThrowAttack()
        {
            // Đứng yên
            rb.velocity = Vector2.zero;


            // Tắt animation chạy
            anim.SetBool("isRunning", false);


            // Quay về phía Player
            FaceTowardsPlayer();


            // Đánh dấu đang thực hiện throw
            isBusy = true;


            // Báo SpearThrower đã ném
            spearThrower.MarkThrown();


            // Trigger animation throw
            anim.SetTrigger("throw");
        }


        // =========================================================
        // END ACTION
        // =========================================================

        // Animation Event:
        // - Cuối attack2
        // - Cuối throw
        public void EndAction()
        {
            // Cho phép AI hoạt động lại
            isBusy = false;
        }


        // =========================================================
        // GIZMOS
        // =========================================================

        private void OnDrawGizmosSelected()
        {
            // =====================================================
            // DETECTION RANGE
            // =====================================================

            // Màu xanh cyan
            Gizmos.color = Color.cyan;

            Gizmos.DrawWireSphere(
                transform.position,
                detectionRange
            );


            // =====================================================
            // SPEAR RANGE
            // =====================================================

            // Màu xanh lá
            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(
                transform.position,
                spearRange
            );


            // =====================================================
            // MELEE RANGE
            // =====================================================

            // Màu vàng
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                transform.position,
                meleeRange
            );


            // =====================================================
            // MELEE HITBOX
            // =====================================================

            if (attackPoint != null)
            {
                // Màu đỏ
                Gizmos.color = Color.red;

                Gizmos.DrawWireSphere(
                    attackPoint.position,
                    meleeRadius
                );
            }
        }
    }
}