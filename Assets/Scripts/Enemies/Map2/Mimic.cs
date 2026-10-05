using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Enemies.Map2
{
    public class Mimic : EnemyBase
    {
        [Header("Mimic AI & State")]
        public bool isAwake = false;         // Ban đầu đóng giả rương báu
        public float wakeRange = 6f;          // Tầm kích hoạt khiến Rương thức giấc
        public float chaseRange = 8f;         // Tầm đuổi theo Musashi
        public float attackRange = 2.5f;      // Tầm táp/cắn cận chiến (Tăng lên 2.5f để đút trúng Collider)
        public float attackCooldown = 1.5f;   // Thời gian hồi chiêu giữa các đòn đánh
        private float lastAttackTime;
        private bool hasDealtDamageThisAttack = false;

        [Header("Mimic Attack Stats")]
        public float attackDamage = 25f;
        public float attackRadius = 1.8f;
        public Transform attackPoint;
        public LayerMask playerLayer;

        [Header("Mimic Movement")]
        public bool facingRight = true;
        public float patrolDistance = 3f;     // Phạm vi đi tuần sau khi thức giấc
        private float startX;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;
        private float lastFlipTime;
        private float flipCooldown = 0.5f;

        [Header("Mimic Audio")]
        public AudioClip wakeSound;
        public AudioClip attackSound;

        protected virtual void Awake()
        {
            maxHealth = 180f;
            currentHealth = maxHealth;
        }

        protected override void Start()
        {
            base.Start();
            facingRight = true;
            startX = transform.position.x;
            moveSpeed = 2.4f + Random.Range(-0.2f, 0.3f);
            maxHealth = 180f;
            currentHealth = maxHealth;
            defense = 40f;
            isAwake = false; // Bắt đầu ở dạng Rương Báu đóng kín
        }

        void Update()
        {
            // Tự động khôi phục máu nếu bị dính 0 từ Inspector
            if (currentHealth <= 0 && maxHealth > 0)
            {
                currentHealth = maxHealth;
            }

            if (playerScript == null || player == null)
            {
                playerScript = FindAnyObjectByType<Player>();
                if (playerScript != null)
                {
                    player = playerScript.transform;
                }
            }

            if (player == null || playerScript == null) return;

            float distanceToPlayer = Vector2.Distance((Vector2)transform.position, (Vector2)player.position);

            // 1. Nếu chưa thức giấc: Kiểm tra Musashi lại gần để mở rương biến hình
            if (!isAwake)
            {
                if (rb != null) rb.velocity = Vector2.zero;

                if (distanceToPlayer <= wakeRange && playerScript.currentHealth > 0)
                {
                    WakeUp();
                }
                return;
            }

            // 2. Sau khi đã thức giấc: Tấn công, Đuổi theo hoặc Đi tuần
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

        private void WakeUp()
        {
            isAwake = true;
            if (anim != null)
            {
                anim.SetTrigger("transform");
            }

            if (audioManager != null && wakeSound != null)
            {
                audioManager.PlayerSFX(wakeSound);
            }
            Debug.Log("Mimic Chest woke up and transformed!");
        }

        private void Flip()
        {
            if (Time.time < lastFlipTime + flipCooldown) return;

            facingRight = !facingRight;
            float absX = Mathf.Abs(transform.localScale.x);
            transform.localScale = new Vector3(facingRight ? absX : -absX, transform.localScale.y, transform.localScale.z);
            lastFlipTime = Time.time;
        }

        private void Patrol()
        {
            if (anim != null) anim.SetBool("isRunning", true);
            float direction = facingRight ? 1f : -1f;
            if (rb != null) rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

            if (patrolDistance > 0)
            {
                if (facingRight && transform.position.x >= startX + patrolDistance)
                {
                    Flip();
                    return;
                }
                else if (!facingRight && transform.position.x <= startX - patrolDistance)
                {
                    Flip();
                    return;
                }
            }

            if (groundLayer.value == 0) return;

            bool isGrounded = true;
            if (groundCheck != null)
            {
                isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.35f, groundLayer);
            }

            bool isWallHit = false;
            if (wallCheck != null)
            {
                isWallHit = Physics2D.OverlapCircle(wallCheck.position, 0.15f, groundLayer);
            }

            if ((groundCheck != null && !isGrounded) || isWallHit)
            {
                Flip();
            }
        }

        private void ChasePlayer()
        {
            if (anim != null) anim.SetBool("isRunning", true);

            float xDiff = player.position.x - transform.position.x;

            if (xDiff > 0.3f && !facingRight)
            {
                Flip();
            }
            else if (xDiff < -0.3f && facingRight)
            {
                Flip();
            }

            float moveDirection = facingRight ? 1f : -1f;
            if (rb != null) rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        }

        private void AttackPlayer()
        {
            if (rb != null) rb.velocity = Vector2.zero;
            if (anim != null) anim.SetBool("isRunning", false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                hasDealtDamageThisAttack = false;
                int attackCombo = Random.Range(1, 3);
                if (anim != null)
                {
                    anim.SetTrigger("attack" + attackCombo);
                    anim.SetTrigger("attack1");
                }
                lastAttackTime = Time.time;
                Debug.Log("Mimic attacking Musashi!");

                // Tự động kích hoạt trừ máu Musashi sau 0.35 giây
                StartCoroutine(DamageRoutine(0.35f));
            }
        }

        private IEnumerator DamageRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            DealDamageToPlayer();
        }

        // Được gọi bởi Animation Event hoặc Coroutine tự động
        public void DealDamageToPlayer()
        {
            if (hasDealtDamageThisAttack) return;
            hasDealtDamageThisAttack = true;

            if (playerScript != null && playerScript.currentHealth > 0)
            {
                playerScript.TakeDamage(attackDamage);
                Debug.Log("Mimic bit Musashi for " + attackDamage + " damage! Remaining HP: " + playerScript.currentHealth);
            }
        }

        private void TriggerAttackSound()
        {
            if (audioManager != null && attackSound != null)
            {
                audioManager.PlayerSFX(attackSound);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (attackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
            }

            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, 0.35f);
            }

            if (wallCheck != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(wallCheck.position, 0.15f);
            }
        }
    }
}
