using UnityEngine;

namespace Assets.Scripts.Enemies.Map2
{
    public class MartialHero3 : EnemyBase
    {
        [Header("MartialHero3 AI & Combat")]
        public float chaseRange = 7.5f;      // Tầm nhìn phát hiện Musashi
        public float attackRange = 1.8f;     // Tầm đánh cận chiến
        public float attackCooldown = 1.8f;  // Thời gian hồi chiêu giữa các đòn đánh
        private float lastAttackTime;

        [Header("MartialHero3 Attack 1 - Đòn nhẹ")]
        public float attack1Damage = 20f;
        public float attack1Radius = 0.8f;
        public Transform attackPoint1;

        [Header("MartialHero3 Attack 2 - Đòn vừa")]
        public float attack2Damage = 30f;
        public float attack2Radius = 1.0f;
        public Transform attackPoint2;

        [Header("MartialHero3 Attack 3 - Đòn mạnh")]
        public float attack3Damage = 40f;
        public float attack3Radius = 1.2f;
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
        }

        void Update()
        {
            if (currentHealth <= 0 || player == null || playerScript == null) return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

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
                // Ngẫu nhiên chọn 1 trong 3 đòn đánh: attack1, attack2, attack3
                int attackCombo = Random.Range(1, 4);
                if (anim != null)
                {
                    anim.SetTrigger("attack" + attackCombo);
                }
                lastAttackTime = Time.time;
            }
        }

        // Được gọi trong Animation Event của Attack1
        public void DealDamageAttack1()
        {
            if (attackPoint1 == null) return;
            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint1.position, attack1Radius, playerLayer);

            if (hitPlayer != null)
            {
                Player p = hitPlayer.GetComponent<Player>();
                if (p != null)
                {
                    p.TakeDamage(attack1Damage);
                    Debug.Log("MartialHero3 hit Musashi (Atk1) for " + attack1Damage + " damage!");
                }
            }
        }

        // Được gọi trong Animation Event của Attack2
        public void DealDamageAttack2()
        {
            if (attackPoint2 == null) return;
            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint2.position, attack2Radius, playerLayer);

            if (hitPlayer != null)
            {
                Player p = hitPlayer.GetComponent<Player>();
                if (p != null)
                {
                    p.TakeDamage(attack2Damage);
                    Debug.Log("MartialHero3 hit Musashi (Atk2) for " + attack2Damage + " damage!");
                }
            }
        }

        // Được gọi trong Animation Event của Attack3
        public void DealDamageAttack3()
        {
            Transform atkPoint = attackPoint3 != null ? attackPoint3 : attackPoint2;
            if (atkPoint == null) return;
            Collider2D hitPlayer = Physics2D.OverlapCircle(atkPoint.position, attack3Radius, playerLayer);

            if (hitPlayer != null)
            {
                Player p = hitPlayer.GetComponent<Player>();
                if (p != null)
                {
                    p.TakeDamage(attack3Damage);
                    Debug.Log("MartialHero3 hit Musashi (Atk3) for " + attack3Damage + " damage!");
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
