using UnityEngine;

namespace Assets.Scripts.Enemies.Map3
{
    // Quái "tự sát": đánh thường (Attack) khi player ở khoảng cách vừa phải,
    // nhưng nếu player áp sát quá gần (explodeRange) thì TỰ KÍCH NỔ (Explosion),
    // gây sát thương diện rộng (AoE) quanh nó rồi tự huỷ - khác với chết thường (Death)
    // do bị đánh tới hết máu.
    public class FireWorm : EnemyBase
    {
        [Header("FireWorm Ranges")]
        public float chaseRange = 7f;     // tầm phát hiện + đuổi theo (Move)
        public float attackRange = 1.6f;  // trong khoảng này thì đánh đòn Attack thường
        public float explodeRange = 0.8f; // áp sát hơn khoảng này thì TỰ NỔ ngay lập tức

        private bool isBusy = false;      // đang đánh Attack thì không làm gì khác
        private bool hasExploded = false; // đảm bảo chỉ tự nổ đúng 1 lần

        [Header("FireWorm Movement")]
        public bool facingRight = true;
        public Transform groundCheck;
        public LayerMask groundLayer;
        public float patrolDistance = 3f;
        private Vector3 startPosition;

        [Header("FireWorm Attack (đòn thường)")]
        public Transform attackPoint;
        public float attackRadius = 0.6f;
        public float attackDamage = 10f;
        public float attackCooldown = 1.8f;
        public float lastAttackTime;

        [Header("FireWorm Explosion (tự nổ)")]
        public float explosionRadius = 2f;     // bán kính vùng nổ, thường rộng hơn nhiều so với attackRadius
        public float explosionDamage = 40f;    // sát thương nổ thường cao hơn đòn thường
        public float explosionDelay = 0.5f;    // thời gian "châm ngòi" trước khi nổ thật sự gây damage (đồng bộ với animation)

        public LayerMask playerLayer;

        [Header("FireWorm Sounds")]
        public AudioClip attackSound;
        public AudioClip explosionSound;


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
            //if (isDead || hasExploded || currentHealth <= 0 || player == null)
            //{
            //    return;
            //}

            // đang đánh đòn thường thì đứng yên, không xét lại trạng thái khác
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

            // ƯU TIÊN CAO NHẤT: áp sát quá gần thì tự nổ ngay, bất kể đang làm gì
            if (distanceToPlayer <= explodeRange)
            {
                SelfDestruct();
            }
            else if (distanceToPlayer <= attackRange)
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
            anim.SetBool("isWalking", true);
            anim.SetBool("isMoving", false);

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
            anim.SetBool("isWalking", false);
            anim.SetBool("isMoving", true);

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
            // di chuyển nhanh hơn patrol 1 chút khi đuổi theo (cảm giác hung hãn hơn)
            rb.velocity = new Vector2(moveDirection * moveSpeed * 1.3f, rb.velocity.y);
        }


        // =========================================================
        // ATTACK (đòn thường)
        // =========================================================

        private void AttackPlayer()
        {
            rb.velocity = Vector2.zero;
            anim.SetBool("isWalking", false);
            anim.SetBool("isMoving", false);
            FaceTowardsPlayer();

            if (isBusy) return;

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                isBusy = true;
                anim.SetTrigger("attack");
                lastAttackTime = Time.time;
            }
        }

        // Gọi bằng Animation Event tại frame đòn Attack chạm player
        public void DealAttackDamage()
        {
            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
            if (hitPlayer != null)
            {
                Player hitPlayerScript = hitPlayer.GetComponentInParent<Player>();
                if (hitPlayerScript != null)
                {
                    hitPlayerScript.TakeDamage(attackDamage);
                }
            }
        }

        private void TriggerAttackSound()
        {
            if (audioManager != null)
            {
                audioManager.PlayerSFX(attackSound);
            }
        }

        // Gọi bằng Animation Event ở frame cuối clip Attack
        public void EndAttack()
        {
            isBusy = false;
        }


        // =========================================================
        // SELF-DESTRUCT (tự nổ khi áp sát quá gần)
        // =========================================================

        private void SelfDestruct()
        {
            if (hasExploded) return; // chỉ kích hoạt đúng 1 lần

            hasExploded = true;
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0f;
            anim.SetBool("isWalking", false);
            anim.SetBool("isMoving", false);
            anim.SetTrigger("explode");

            col.enabled = false; // tắt va chạm ngay khi bắt đầu châm ngòi, tránh bị đánh/đẩy lung tung

            // gây damage sau 1 khoảng trễ (đồng bộ với animation "châm ngòi" trước khi nổ thật)
            Invoke(nameof(DealExplosionDamage), explosionDelay);
        }

        // Có thể gọi qua Invoke (như trên) HOẶC qua Animation Event tại đúng frame nổ -
        // nếu dùng Animation Event thì xoá dòng Invoke ở SelfDestruct() để tránh gây damage 2 lần
        private void DealExplosionDamage()
        {
            Collider2D hitPlayer = Physics2D.OverlapCircle(transform.position, explosionRadius, playerLayer);
            if (hitPlayer != null)
            {
                Player hitPlayerScript = hitPlayer.GetComponentInParent<Player>();
                if (hitPlayerScript != null)
                {
                    hitPlayerScript.TakeDamage(explosionDamage);
                }
            }

            if (audioManager != null && explosionSound != null)
            {
                audioManager.PlayerSFX(explosionSound);
            }
        }

        // Gọi bằng Animation Event ở frame cuối clip Explosion, để tự huỷ object sau khi hiệu ứng nổ chạy xong
        public void DestroyAfterExplosion()
        {
            Destroy(gameObject);
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, chaseRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explodeRange);

            Gizmos.color = new Color(1f, 0.5f, 0f); // cam, vùng nổ AoE
            Gizmos.DrawWireSphere(transform.position, explosionRadius);

            if (attackPoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
            }
        }
    }
}