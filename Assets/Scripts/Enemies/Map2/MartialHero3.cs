using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Enemies.Map2
{
    public class MartialHero3 : EnemyBase
    {
        [Header("MartialHero3 AI & Combat")]
        public float chaseRange = 7.5f;      // Tầm nhìn phát hiện Musashi
        public float attackRange = 2.2f;     // Tầm đánh cận chiến
        public float attackCooldown = 1.8f;  // Thời gian hồi chiêu giữa các đòn đánh
        private float lastAttackTime;
        private bool hasDealtDamageThisAttack = false;

        [Header("MartialHero3 Attack 1 - Đòn nhẹ")]
        public float attack1Damage = 20f;
        public float attack1Radius = 1.5f;
        public Transform attackPoint1;

        [Header("MartialHero3 Attack 2 - Đòn vừa")]
        public float attack2Damage = 30f;
        public float attack2Radius = 1.6f;
        public Transform attackPoint2;

        [Header("MartialHero3 Attack 3 - Đòn mạnh")]
        public float attack3Damage = 40f;
        public float attack3Radius = 1.8f;
        public Transform attackPoint3;

        public LayerMask playerLayer;

        [Header("MartialHero3 Movement")]
        public bool facingRight = true;
        public float patrolDistance = 4f;   // Phạm vi đi tuần
        private float startX;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;
        private float lastFlipTime;
        private float flipCooldown = 0.5f;

        [Header("MartialHero3 Audio")]
        public AudioClip attack1Sound;
        public AudioClip attack2Sound;
        public AudioClip attack3Sound;

        protected override void Start()
        {
            base.Start();
            facingRight = true;
            startX = transform.position.x;
            moveSpeed = 2.5f + Random.Range(-0.2f, 0.3f);
            maxHealth = 160f;
            currentHealth = maxHealth;
            defense = 50f;
            attackRange = Mathf.Max(attackRange, 2.2f);

            if (groundLayer.value == 0)
            {
                groundLayer = LayerMask.GetMask("Ground");
            }
            if (playerLayer.value == 0)
            {
                playerLayer = LayerMask.GetMask("Player");
            }
        }

        void Update()
        {
            if (currentHealth <= 0) return;
            if (!EnsurePlayerReference()) return;

            float xDiff = player.position.x - transform.position.x;
            float absXDiff = Mathf.Abs(xDiff);
            float yDiff = Mathf.Abs(player.position.y - transform.position.y);
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // 1. Tấn công nếu trong tầm đánh ngang và cùng sàn
            if (absXDiff <= attackRange && yDiff <= 2.2f && playerScript.currentHealth > 0)
            {
                AttackPlayer();
            }
            // 2. Đuổi theo nếu trong tầm nhìn
            else if (distanceToPlayer <= chaseRange && yDiff <= 4.5f && playerScript.currentHealth > 0)
            {
                ChasePlayer();
            }
            // 3. Tuần tra bình thường
            else
            {
                Patrol();
            }
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
                isWallHit = Physics2D.OverlapCircle(wallCheck.position, 0.2f, groundLayer);
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

            if (xDiff > 0.2f && !facingRight)
            {
                Flip();
            }
            else if (xDiff < -0.2f && facingRight)
            {
                Flip();
            }

            float moveDirection = facingRight ? 1f : -1f;
            if (rb != null) rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        }

        private void AttackPlayer()
        {
            if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
            if (anim != null) anim.SetBool("isRunning", false);

            float xDiff = player.position.x - transform.position.x;
            if (xDiff > 0.1f && !facingRight)
            {
                Flip();
            }
            else if (xDiff < -0.1f && facingRight)
            {
                Flip();
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                hasDealtDamageThisAttack = false;
                int attackCombo = Random.Range(1, 4);
                if (anim != null)
                {
                    anim.SetTrigger("attack" + attackCombo);
                }
                TriggerAttackSound(attackCombo);
                lastAttackTime = Time.time;
                StartCoroutine(DamageRoutine(0.35f, attackCombo));
            }
        }

        private IEnumerator DamageRoutine(float delay, int combo)
        {
            yield return new WaitForSeconds(delay);
            if (combo == 1) DealDamageAttack1();
            else if (combo == 2) DealDamageAttack2();
            else DealDamageAttack3();
        }

        private void TriggerAttackSound(int combo)
        {
            if (combo == 1) TriggerAttack1Sound();
            else if (combo == 2) TriggerAttack2Sound();
            else TriggerAttack3Sound();
        }

        // Được gọi trong Animation Event của Attack1 hoặc từ DamageRoutine
        public void DealDamageAttack1()
        {
            if (hasDealtDamageThisAttack) return;
            hasDealtDamageThisAttack = true;

            ApplyDamage(attackPoint1, attack1Radius, attack1Damage);
        }

        // Được gọi trong Animation Event của Attack2 hoặc từ DamageRoutine
        public void DealDamageAttack2()
        {
            if (hasDealtDamageThisAttack) return;
            hasDealtDamageThisAttack = true;

            ApplyDamage(attackPoint2, attack2Radius, attack2Damage);
        }

        // Được gọi trong Animation Event của Attack3 hoặc từ DamageRoutine
        public void DealDamageAttack3()
        {
            if (hasDealtDamageThisAttack) return;
            hasDealtDamageThisAttack = true;

            Transform atkPoint = attackPoint3 != null ? attackPoint3 : attackPoint2;
            ApplyDamage(atkPoint, attack3Radius, attack3Damage);
        }

        private void ApplyDamage(Transform atkPoint, float radius, float damage)
        {
            bool hit = false;
            if (atkPoint != null)
            {
                float rad = radius > 0.2f ? radius : 1.6f;
                Collider2D[] hits = Physics2D.OverlapCircleAll(atkPoint.position, rad, playerLayer);
                foreach (var col in hits)
                {
                    Player p = col.GetComponentInParent<Player>();
                    if (p != null && p.currentHealth > 0)
                    {
                        p.TakeDamage(damage);
                        hit = true;
                        break;
                    }
                }
            }

            // Fallback nếu OverlapCircle bị lệch
            if (!hit && playerScript != null && playerScript.currentHealth > 0)
            {
                float absX = Mathf.Abs(player.position.x - transform.position.x);
                float yDiff = Mathf.Abs(player.position.y - transform.position.y);
                if (absX <= attackRange + 0.6f && yDiff <= 2.2f)
                {
                    playerScript.TakeDamage(damage);
                }
            }
        }

        private void TriggerAttack1Sound()
        {
            if (audioManager != null && attack1Sound != null)
            {
                audioManager.PlayerSFX(attack1Sound);
            }
        }

        private void TriggerAttack2Sound()
        {
            if (audioManager != null && attack2Sound != null)
            {
                audioManager.PlayerSFX(attack2Sound);
            }
        }

        private void TriggerAttack3Sound()
        {
            if (audioManager != null && attack3Sound != null)
            {
                audioManager.PlayerSFX(attack3Sound);
            }
        }

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

            if (attackPoint3 != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(attackPoint3.position, attack3Radius);
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
