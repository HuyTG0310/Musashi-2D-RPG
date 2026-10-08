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
        private bool isAttacking = false;

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

            // =====================================================
            // ĐANG ATTACK
            // =====================================================

            // Trong toàn bộ combo Attack1 -> Attack2:
            // tuyệt đối không di chuyển theo Player
            if (isAttacking)
            {
                rb.velocity = Vector2.zero;
                anim.SetBool("isRunning", false);
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
            // PLAYER CÒN SỐNG
            // =====================================================

            if (playerScript.currentHealth <= 0)
            {
                Patrol();
                return;
            }


            // =====================================================
            // ATTACK
            // =====================================================

            if (distanceToPlayer <= attackRange)
            {
                AttackPlayer();
            }


            // =====================================================
            // CHASE
            // =====================================================

            else if (distanceToPlayer <= chaseRange)
            {
                ChasePlayer();
            }


            // =====================================================
            // PATROL
            // =====================================================

            else
            {
                Patrol();
            }
        }

        // method đổi hướng di chuyển
        private void Flip()
        {
            facingRight = !facingRight;
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, 1);
        }

        // method đi tuần tra trong 1 khoảng cách cố định quanh điểm spawn
        private void Patrol()
        {
            anim.SetBool("isRunning", true);

            // di chuyển theo hướng hiện tại
            float direction = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

            // nếu đi quá xa bên phải điểm gốc thì quay đầu
            if (transform.position.x >= startPosition.x + patrolDistance && facingRight)
            {
                Flip();
            }
            // nếu đi quá xa bên trái điểm gốc thì quay đầu
            else if (transform.position.x <= startPosition.x - patrolDistance && !facingRight)
            {
                Flip();
            }

            // vẫn giữ groundCheck để tránh đi lọt khỏi bệ / rơi xuống hố
            bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
            if (!isGrounded)
            {
                Flip();
            }
        }


        // method đuổi theo player
        private void ChasePlayer()
        {
            anim.SetBool("isRunning", true);

            // khoảng đệm để tránh Flip liên tục khi đứng gần sát player
            float flipDeadzone = 0.15f;
            float xDifference = player.position.x - transform.position.x;

            // chỉ đổi hướng khi chênh lệch đủ lớn, tránh lật qua lật lại liên tục
            if (xDifference > flipDeadzone && !facingRight)
            {
                Flip();
            }
            else if (xDifference < -flipDeadzone && facingRight)
            {
                Flip();
            }

            // di chuyển theo hướng đã xác định
            float moveDirection = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        }

        // hàm kích hoạt animation tấn công player, chọn đòn nhẹ hoặc đòn mạnh
        private void AttackPlayer()
        {
            rb.velocity = Vector2.zero;
            anim.SetBool("isRunning", false);

            // Quay mặt về Player
            float xDifference =
                player.position.x - transform.position.x;

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

                anim.SetTrigger("attack1");

                lastAttackTime = Time.time;
            }
        }
        

        // được gọi trong animation Attack1 (đòn nhẹ) của MartialHero2
        public void DealDamageAttack1()
        {
            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint1.position, attack1Radius, playerLayer);

            if (hitPlayer != null)
            {
                Player playerHit = hitPlayer.GetComponent<Player>();
                playerHit.TakeDamage(attack1Damage);
            }
        }

        // được gọi trong animation Attack2 (đòn mạnh) của MartialHero2
        public void DealDamageAttack2()
        {
            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint2.position, attack2Radius, playerLayer);

            if (hitPlayer != null)
            {
                Player playerHit = hitPlayer.GetComponent<Player>();
                playerHit.TakeDamage(attack2Damage);
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


        private void TriggerAttack1Sound()
        {
            if (audioManager != null)
            {
                audioManager.PlayerSFX(attack1Sound);
            }
        }

        private void TriggerAttack2Sound()
        {
            if (audioManager != null)
            {
                audioManager.PlayerSFX(attack2Sound);
            }
        }
        public void EndAttack()
        {
            isAttacking = false;
        }
    }
}