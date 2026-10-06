using UnityEngine;

namespace Assets.Scripts.Enemies.Map3
{
    // Phiên bản CHỈ CẬN CHIẾN (chưa có ném spear).
    // Combo 2 đòn liên tiếp: Any State -> attack1 -> attack2 -> Idle (Animator tự nối bằng Exit Time).
    // Phần ném spear sẽ làm riêng sau, xem file SpearThrower.cs.
    public class Huntress1 : EnemyBase
    {
        [Header("Huntress1 Ranges")]
        public float chaseRange = 8f;    // tầm phát hiện + đuổi theo player
        public float meleeRange = 1.3f;  // player trong khoảng này thì chuyển sang combo cận chiến

        // đang thực hiện combo cận chiến thì không làm gì khác
        private bool isBusy = false;

        [Header("Huntress1 Movement")]
        public bool facingRight = true;
        public Transform groundCheck;
        public LayerMask groundLayer;
        public float patrolDistance = 3f;
        private Vector3 startPosition;

        [Header("Huntress1 Attack Point")]
        public Transform attackPoint;

        [Header("Huntress1 Melee (2 đòn liên tiếp: attack1 -> attack2)")]
        public float meleeRadius = 0.6f;
        public float meleeDamageHit1 = 10f;
        public float meleeDamageHit2 = 15f;
        public float meleeCooldown = 2f;  // hồi chiêu giữa 2 lần bắt đầu combo (không phải giữa attack1 và attack2 trong 1 lần đánh)
        public float lastMeleeTime;

        public LayerMask playerLayer;

        [Header("Huntress1 Sounds")]
        public AudioClip meleeSound;


        protected override void Start()
        {
            base.Start();
            facingRight = true;
            float randomOffset = Random.Range(-0.5f, 0.5f);
            moveSpeed += randomOffset;
            startPosition = transform.position;
        }


        void FixedUpdate()
        {
            //if (isDead || currentHealth <= 0 || player == null)
            //{
            //    return;
            //}

            // đang thực hiện combo thì đứng yên, không xét lại trạng thái khác
            if (isBusy)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (playerScript.currentHealth <= 0)
            {
                Patrol();
                return;
            }

            if (distanceToPlayer <= meleeRange)
            {
                MeleeAttack();
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


        private void Flip()
        {
            facingRight = !facingRight;
            transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
        }


        private void FaceTowardsPlayer()
        {
            float xDifference = player.position.x - transform.position.x;
            if (xDifference > 0 && !facingRight)
            {
                Flip();
            }
            else if (xDifference < 0 && facingRight)
            {
                Flip();
            }
        }


        private void Patrol()
        {
            anim.SetBool("isRunning", true);

            float direction = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

            if (transform.position.x >= startPosition.x + patrolDistance && facingRight)
            {
                Flip();
            }
            else if (transform.position.x <= startPosition.x - patrolDistance && !facingRight)
            {
                Flip();
            }

            bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
            if (!isGrounded)
            {
                Flip();
            }
        }


        private void ChasePlayer()
        {
            anim.SetBool("isRunning", true);

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


        // =========================================================
        // MELEE COMBO (attack1 -> attack2 liên tiếp)
        // =========================================================

        private void MeleeAttack()
        {
            rb.velocity = Vector2.zero;
            anim.SetBool("isRunning", false);
            FaceTowardsPlayer();

            if (isBusy) return;

            if (Time.time >= lastMeleeTime + meleeCooldown)
            {
                isBusy = true;

                // chỉ cần bắn trigger attack1 để khởi động combo,
                // Animator sẽ tự động nối tiếp attack1 -> attack2 (Exit Time, không cần code can thiệp)
                anim.SetTrigger("attack1");
                lastMeleeTime = Time.time;
            }
        }

        // Gọi bằng Animation Event tại đúng frame của clip attack1 lúc vũ khí chạm player
        public void DealMeleeDamage1()
        {
            DealMeleeDamage(meleeDamageHit1);
        }

        // Gọi bằng Animation Event tại đúng frame của clip attack2 lúc vũ khí chạm player
        public void DealMeleeDamage2()
        {
            DealMeleeDamage(meleeDamageHit2);
        }

        private void DealMeleeDamage(float damageAmount)
        {
            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, meleeRadius, playerLayer);
            if (hitPlayer != null)
            {
                Player hitPlayerScript = hitPlayer.GetComponentInParent<Player>();
                if (hitPlayerScript != null)
                {
                    hitPlayerScript.TakeDamage(damageAmount);
                }
            }
        }

        private void TriggerMeleeSound()
        {
            if (audioManager != null)
            {
                audioManager.PlayerSFX(meleeSound);
            }
        }


        // Gọi bằng Animation Event ở frame cuối cùng của clip attack2 (kết thúc combo)
        public void EndAction()
        {
            isBusy = false;
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, meleeRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, chaseRange);

            if (attackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, meleeRadius);
            }
        }
    }
}