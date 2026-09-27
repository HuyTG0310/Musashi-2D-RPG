using UnityEngine;

namespace Assets.Scripts.Enemies.Map1
{
    public class Rat : EnemyBase
    {
        [Header("Rat AI & Combat")]
        public float chaseRange = 5f;       // tầm nhìn phát hiện
        public float attackRange = 1.2f;    // tầm đánh
        public float attackCooldown = 2f;   // thời gian hồi chiêu
        public float attackDamage = 20f;    // sát thương
        public float lastAttackTime;

        [Header("Rat Movement")]
        public bool facingRight = true;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;

        [Header("Rat Attack")]
        public Transform attackPoint;
        public float attackRadius = 0.5f;
        public LayerMask playerLayer;


        [Header("Rat Sounds")]
        public AudioClip attackSound;

        protected override void Start()
        {
            base.Start();
            facingRight = true;
            float randomOffset = Random.Range(-0.5f, 0.5f);
            moveSpeed += randomOffset;
        }


        void Update()
        {
            if (currentHealth <= 0 || player == null)
            {
                return;
            }

            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
           
            if (stateInfo.IsName("Hurt"))
            {
                rb.velocity = Vector2.zero; // Ép đứng im ngay lập tức
                return; // Thoát hàm Update, ngắt hoàn toàn logic đuổi/tuần tra bên dưới
            }

            // tính khoảng cách đến player
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);


            // nếu trong vùng tấn công và player còn sống
            if (distanceToPlayer <= attackRange && playerScript.currentHealth > 0)
            {
                AttackPlayer();
            }
            // nếu nằm trong khoảng đuổi theo player và player còn sống
            else if (distanceToPlayer <= chaseRange && playerScript.currentHealth > 0)
            {
                ChasePlayer();
            }
            // còn lại nằm ngoài vùng hoặc player chết thì đi tuần
            else
            {
                Patrol();
            }
        }

        // method đổi hướng di chuyển
        private void Flip()
        {
            facingRight = !facingRight;
            transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
        }

        // method đi tuần tra
        private void Patrol()
        {
            anim.SetBool("isRunning", true);

            // di chuyển theo hướng hiện tại
            float direction = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

            bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
            bool isWallHit = Physics2D.OverlapCircle(wallCheck.position, 0.2f, groundLayer);

            // nếu gần té hoặc chạm tường thì đổi hướng
            if (!isGrounded || isWallHit)
            {

                Debug.Log($"isGrounded {isGrounded}, isWallHit {isWallHit}");
                Flip();
            }
        }


        // method đuổi theo Musashi
        private void ChasePlayer()
        {
            anim.SetBool("isRunning", true);

            // đổi hướng về phía Musashi
            if (player.position.x > transform.position.x && !facingRight)
            {
                Flip();
            }
            else if (player.position.x < transform.position.x && facingRight)
            {
                Flip();
            }

            // di chuyển theo hướng đã xác định
            float moveDirection = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        }

        // hàm kích hoạt animation tấn công player
        private void AttackPlayer()
        {
            // lặp tức đứng yên ko di chuyển
            rb.velocity = Vector2.zero;
            anim.SetBool("isRunning", false);

            // đủ thời gian hồi chiêu thì tấn công
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                anim.SetTrigger("attack");
                lastAttackTime = Time.time;
            }
        }

        // được gọi trong animation atk của Rat
        public void DealDamageToPlayer()
        {
            // kiểm tra xem có va chạm player ko
            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);

            // nếu đánh trúng player thì gọi hàm player bị tấn công
            if (hitPlayer != null)
            {
                Player playerScript = hitPlayer.GetComponent<Player>();
                playerScript.TakeDamage(attackDamage);
            }
        }

        // method vẽ vòng tròn hitbox để dễ quan sát
        private void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }


        private void TriggerAtkHandSound()
        {
            if (audioManager != null)
            {
                audioManager.PlayerSFX(attackSound);
            }
        }
    }
}
