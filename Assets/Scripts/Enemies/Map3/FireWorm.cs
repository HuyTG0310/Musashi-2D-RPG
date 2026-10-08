using UnityEngine;

namespace Assets.Scripts.Enemies.Map3
{
    // FireWorm: patrol/đứng chờ, khi player vào tầm thì đứng yên phun ra 1 viên lửa (FireProjectile).
    // Bản thân FireWorm KHÔNG tự nổ - chỉ viên lửa nó phun ra mới nổ (xem FireProjectile.cs).
    public class FireWorm : EnemyBase
    {
        [Header("FireWorm Ranges")]
        public float chaseRange = 6f;    // tầm phát hiện player (đồng thời cũng là tầm phun lửa)
        public float patrolDistance = 3f;

        private bool isBusy = false; // đang phun lửa thì đứng yên, không patrol

        [Header("FireWorm Movement")]
        public bool facingRight = true;
        public Transform groundCheck;
        public LayerMask groundLayer;
        private Vector3 startPosition;

        [Header("FireWorm Attack (phun lửa)")]
        public Transform firePoint;           // vị trí viên lửa xuất hiện (miệng FireWorm)
        public GameObject fireProjectilePrefab;
        public float attackCooldown = 2.5f;
        public float lastAttackTime;

        public LayerMask playerLayer;

        [Header("FireWorm Sounds")]
        public AudioClip attackSound;


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

            // player trong tầm phát hiện thì đứng yên phun lửa, ngoài tầm thì patrol
            if (distanceToPlayer <= chaseRange)
            {
                Debug.Log(gameObject.name + ": player cách " + distanceToPlayer.ToString("F2") + " (trong chaseRange=" + chaseRange + ") -> ATTACK");
                AttackPlayer();
            }
            else
            {
                Debug.Log(gameObject.name + ": player cách " + distanceToPlayer.ToString("F2") + " (ngoài chaseRange=" + chaseRange + ") -> PATROL");
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


        // =========================================================
        // ATTACK (phun lửa)
        // =========================================================

        private void AttackPlayer()
        {
            rb.velocity = Vector2.zero;
            anim.SetBool("isWalking", false);
            FaceTowardsPlayer();

            if (isBusy) return;

            if (Time.time >= lastAttackTime + attackCooldown)  // chưa đủ cooldown thì không bắn tiếp
            {
                isBusy = true;
                anim.SetTrigger("attack");
                lastAttackTime = Time.time;
            }
        }

        // Gọi bằng Animation Event tại đúng frame FireWorm phun lửa ra
        public void SpawnFireProjectile()
        {
            Debug.Log(gameObject.name + ": SpawnFireProjectile() được gọi lúc " + Time.time);

            if (fireProjectilePrefab == null || firePoint == null)
            {
                Debug.LogWarning(gameObject.name + ": chưa gán fireProjectilePrefab hoặc firePoint! prefab=" + fireProjectilePrefab + " firePoint=" + firePoint);
                return;
            }

            GameObject fireObj = Instantiate(fireProjectilePrefab, firePoint.position, Quaternion.identity);
            Debug.Log(gameObject.name + ": Đã Instantiate lửa tại " + firePoint.position);
            FireProjectile fireScript = fireObj.GetComponent<FireProjectile>();

            if (fireScript != null)
            {
                float direction = facingRight ? 1f : -1f;
                fireScript.SetDirection(direction);
                fireScript.playerLayer = playerLayer;
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


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, chaseRange);

            if (firePoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(firePoint.position, 0.2f);
            }
        }
    }
}