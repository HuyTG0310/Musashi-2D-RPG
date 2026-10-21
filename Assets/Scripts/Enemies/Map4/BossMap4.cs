using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Enemies.Map4
{
    public class BossMap4 : EnemyBase
    {
        public enum BossPhase
        {
            Phase1_Normal,
            Transforming,
            Phase2_FlamingEnrage
        }

        public enum BossActionState
        {
            Idle,
            Chase,
            Attacking,
            DashStriking,
            Backstepping,
            Defending,
            CounterAttacking,
            JumpAttacking,
            Shouting,
            Dead
        }

        [Header("--- BOSS GENERAL SETTINGS ---")]
        public string bossName = "LÃNH CHÚA SA MẠC - TOSHIZO";
        public BossPhase currentPhase = BossPhase.Phase1_Normal;
        public BossActionState actionState = BossActionState.Idle;

        [Header("--- COMBAT RANGES & TIMERS ---")]
        public float chaseRange = 13f;
        public float meleeAttackRange = 3.2f;
        public float dashStrikeMinDistance = 3.8f;
        public float dashStrikeMaxDistance = 7.0f;
        public float jumpAttackMinDistance = 4.5f;
        public float jumpAttackMaxDistance = 9.5f;
        public float attackCooldown = 1.35f;
        private float lastAttackTime = -99f;
        private float lastBackstepTime = -99f;
        public float backstepCooldown = 4.0f;

        [Header("--- DAMAGE SETTINGS (BALANCED FOR TESTING) ---")]
        public float attack1Damage = 6f;
        public float attack2Damage = 8f;
        public float attack3Damage = 12f;
        public float jumpAttackDamage = 10f;
        public float dashStrikeDamage = 10f;
        public float shockwaveDamage = 8f;
        public float counterDamage = 6f;

        [Header("--- DEFENSE & ENRAGE MECHANICS ---")]
        [Range(0, 100)] public float defendChance = 40f; // Tỉ lệ giơ kiếm đỡ đòn khi bị đánh dồn dập
        public float defendDuration = 1.0f;
        public bool isInvulnerable = false; // Bất tử khi đang biến hình Shout
        private int consecutiveHitsTaken = 0;
        private bool hasTransformed = false;
        private Coroutine defendCoroutine;

        [Header("--- MOVEMENT & PHYSICS ---")]
        public bool facingRight = true;
        public float enragedSpeedMultiplier = 1.45f;
        public float jumpForceX = 7f;
        public float jumpForceY = 5.2f;
        public Transform groundCheck;
        public Transform wallCheck;
        public LayerMask groundLayer;

        [Header("--- HITBOX SETTINGS ---")]
        public Transform meleeAttackPoint;
        public float meleeAttackRadius = 2.2f;
        public Transform jumpSlamPoint;
        public float jumpSlamRadius = 2.0f;
        public LayerMask playerLayer;

        [Header("--- AUDIO SETTINGS ---")]
        public AudioClip attackSwingSound;
        public AudioClip flameSlashSound;
        public AudioClip jumpLandSound;
        public AudioClip defendBlockSound;
        public AudioClip roarShoutSound;

        [Header("--- UI BOSS HEALTH BAR ---")]
        public Slider bossSliderUI;
        public Image bossFillImage;
        public GameObject bossUIParent;
        public Text bossNameText;

        [Header("--- VISUAL FX & POLISH ---")]
        public Color phase2AuraColor = new Color(1f, 0.45f, 0.1f, 1f);
        public bool enableGhostAfterimage = true;

        private float normalMoveSpeed;
        private SpriteRenderer bossSpriteRenderer;
        private Coroutine passiveFlameAuraCoroutine;

        protected override void Start()
        {
            base.Start();

            // Chỉ số trùm cuối
            maxHealth = 1200f;
            currentHealth = maxHealth;
            defense = 25f;
            normalMoveSpeed = moveSpeed > 0 ? moveSpeed : 2.8f;
            moveSpeed = normalMoveSpeed;

            minCoins = 20;
            maxCoins = 40;

            bossSpriteRenderer = GetComponent<SpriteRenderer>();

            if (bossUIParent != null)
            {
                bossUIParent.SetActive(false);
            }

            if (bossNameText != null)
            {
                bossNameText.text = bossName;
            }

            UpdateBossUI();
        }

        private void Update()
        {
            if (currentHealth <= 0 || player == null || playerScript == null)
            {
                return;
            }

            // Tính toán khoảng cách chuẩn xác theo trọng tâm Boss và Musashi
            float horizontalDistance = GetHorizontalDistToPlayer();
            float verticalDistance = GetVerticalDistToPlayer();
            float distanceToPlayer = Vector2.Distance(GetBossCenter(), GetPlayerCenter());

            // Kích hoạt thanh máu Boss khi Musashi bước vào khu vực giao tranh
            if (horizontalDistance <= chaseRange && bossUIParent != null && !bossUIParent.activeSelf)
            {
                bossUIParent.SetActive(true);
            }

            // Nếu đang bận thi triển kỹ năng thì không xử lý di chuyển AI thông thường
            if (actionState == BossActionState.Attacking ||
                actionState == BossActionState.DashStriking ||
                actionState == BossActionState.Backstepping ||
                actionState == BossActionState.CounterAttacking ||
                actionState == BossActionState.JumpAttacking ||
                actionState == BossActionState.Shouting ||
                actionState == BossActionState.Defending ||
                actionState == BossActionState.Dead)
            {
                return;
            }

            // Kích hoạt biến hình nộ khi máu tụt xuống <= 50%
            if (!hasTransformed && currentHealth <= (maxHealth * 0.5f))
            {
                StartCoroutine(EnrageTransformationRoutine());
                return;
            }

            // Luôn quay mặt chuẩn về phía Musashi khi tự do
            LookAtPlayer();

            // Xử lý AI hành vi theo khoảng cách và giai đoạn
            HandleAIBehavior(distanceToPlayer, horizontalDistance, verticalDistance);
        }

        private Vector2 GetBossCenter() => (Vector2)transform.position + Vector2.up * 1.6f;
        private Vector2 GetPlayerCenter() => player != null ? ((Vector2)player.position + Vector2.down * 1.0f) : (Vector2)transform.position;
        private float GetHorizontalDistToPlayer() => player != null ? Mathf.Abs(player.position.x - transform.position.x) : 999f;
        private float GetVerticalDistToPlayer() => player != null ? Mathf.Abs(GetBossCenter().y - GetPlayerCenter().y) : 999f;

        private void LookAtPlayer()
        {
            if (player == null) return;
            if (player.position.x > transform.position.x && !facingRight)
            {
                Flip();
            }
            else if (player.position.x < transform.position.x && facingRight)
            {
                Flip();
            }
        }

        private void Flip()
        {
            facingRight = !facingRight;
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        }

        #region --- AI COMBAT LOGIC & COMBOS ---

        private void HandleAIBehavior(float distanceToPlayer, float horizontalDist, float verticalDist)
        {
            float actualCooldown = currentPhase == BossPhase.Phase2_FlamingEnrage ? attackCooldown * 0.65f : attackCooldown;
            bool canAttack = Time.time >= (lastAttackTime + actualCooldown);
            bool canBackstep = Time.time >= (lastBackstepTime + backstepCooldown);

            // 1. TÌNH HUỐNG LÙI BƯỚC NÉ ĐÒN (Tactical Backstep):
            // Khi Musashi áp sát quá gần (< 2.2m) trong lúc Boss đang hồi chiêu
            if (!canAttack && canBackstep && horizontalDist <= 2.2f && verticalDist <= 1.5f && Random.value < 0.35f)
            {
                StartCoroutine(ExecuteTacticalBackstep());
                return;
            }

            // 2. TÌNH HUỐNG CẬN CHIẾN (Melee Attack / Multi-Hit Combo):
            if (horizontalDist <= meleeAttackRange && verticalDist <= 2.8f)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                anim.SetBool("isRunning", false);

                if (canAttack)
                {
                    StartCoroutine(ExecuteMeleeAttackCombo());
                }
                return;
            }

            // 3. TÌNH HUỐNG LƯỚT KIẾM ÁP SÁT SIÊU TỐC (Dash Strike - Độc quyền Phase 2):
            if (currentPhase == BossPhase.Phase2_FlamingEnrage &&
                horizontalDist >= dashStrikeMinDistance && horizontalDist <= dashStrikeMaxDistance &&
                canAttack && Random.value < 0.45f)
            {
                StartCoroutine(ExecuteDashStrike());
                return;
            }

            // 4. TÌNH HUỐNG NHẢY BỔ ÁP SÁT (Jump Plunge Attack):
            if (horizontalDist >= jumpAttackMinDistance && horizontalDist <= jumpAttackMaxDistance &&
                canAttack && Random.value < 0.60f)
            {
                StartCoroutine(ExecuteJumpAttack());
                return;
            }

            // 5. TRUY ĐUỔI MUSASHI (Chase):
            if (horizontalDist <= chaseRange)
            {
                actionState = BossActionState.Chase;
                anim.SetBool("isRunning", true);

                float direction = (player.position.x > transform.position.x) ? 1f : -1f;
                rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
                return;
            }

            // 6. NGOÀI TẦM NHÌN (Idle):
            actionState = BossActionState.Idle;
            anim.SetBool("isRunning", false);
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        // Chuỗi đòn đánh cận chiến thông minh (Branching Combos)
        private IEnumerator ExecuteMeleeAttackCombo()
        {
            actionState = BossActionState.Attacking;
            rb.velocity = Vector2.zero;
            anim.SetBool("isRunning", false);

            if (currentPhase == BossPhase.Phase1_Normal)
            {
                // PHASE 1: Kiếm Đạo Samurai
                float roll = Random.value;

                if (roll < 0.35f)
                {
                    // Đòn đơn 1: Chém ngang
                    yield return StartCoroutine(PerformAttack(1, attack1Damage, 0.35f, 0.35f));
                }
                else if (roll < 0.65f)
                {
                    // Combo 2 đòn liên hoàn: Chém 1 -> Chém 2
                    yield return StartCoroutine(PerformAttack(1, attack1Damage, 0.35f, 0.20f));
                    StepForward(1.5f);
                    yield return StartCoroutine(PerformAttack(2, attack2Damage, 0.30f, 0.35f));
                }
                else
                {
                    // Trọng Kiếm Đâm Phá Giáp (Attack 3): Cảnh báo flash vàng rồi đâm cực mạnh
                    yield return StartCoroutine(FlashColorRoutine(new Color(1f, 0.9f, 0.3f), 0.2f));
                    StepForward(2.2f);
                    yield return StartCoroutine(PerformAttack(3, attack3Damage, 0.40f, 0.40f));
                }
            }
            else
            {
                // PHASE 2: Hỏa Kiếm Thần Ma Cuồng Nộ
                float roll = Random.value;

                if (roll < 0.40f)
                {
                    // Hỏa Long Song Trảm: Chém lửa 1 -> Chém lửa 2
                    yield return StartCoroutine(PerformAttack(1, attack1Damage, 0.28f, 0.18f));
                    StepForward(2.0f);
                    yield return StartCoroutine(PerformAttack(2, attack2Damage, 0.25f, 0.25f));
                }
                else if (roll < 0.80f)
                {
                    // ĐẠI COMBO 3 ĐÒN CUỒNG BẠO: Chém 1 -> Chém 2 -> Trọng Hỏa Kiếm Đâm Kết Liễu
                    yield return StartCoroutine(PerformAttack(1, attack1Damage, 0.25f, 0.15f));
                    StepForward(1.8f);
                    yield return StartCoroutine(PerformAttack(2, attack2Damage, 0.25f, 0.15f));
                    StepForward(2.5f);
                    StartCoroutine(TriggerScreenShake(0.2f, 0.15f));
                    yield return StartCoroutine(PerformAttack(3, attack3Damage, 0.35f, 0.35f));
                }
                else
                {
                    // Đòn Chém Nặng Đột Kích (Attack 3 Fire)
                    yield return StartCoroutine(FlashColorRoutine(Color.red, 0.15f));
                    StepForward(3.0f);
                    yield return StartCoroutine(PerformAttack(3, attack3Damage, 0.35f, 0.35f));
                }
            }

            lastAttackTime = Time.time;
            actionState = BossActionState.Idle;
        }

        private IEnumerator PerformAttack(int attackIndex, float damage, float windupTime, float recoveryTime)
        {
            anim.SetTrigger("attack" + attackIndex);
            PlaySound(currentPhase == BossPhase.Phase2_FlamingEnrage ? flameSlashSound : attackSwingSound);

            yield return new WaitForSeconds(windupTime);
            DealMeleeDamage(damage);
            yield return new WaitForSeconds(recoveryTime);
        }

        private void StepForward(float force)
        {
            float dir = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(dir * force, rb.velocity.y);
            if (enableGhostAfterimage)
            {
                SpawnAfterimage(currentPhase == BossPhase.Phase2_FlamingEnrage ? phase2AuraColor : new Color(0.8f, 0.8f, 0.8f, 0.6f), 0.3f);
            }
        }

        // Kỹ năng Lướt Kiếm Áp Sát Siêu Tốc (Dash Strike - Phase 2)
        private IEnumerator ExecuteDashStrike()
        {
            actionState = BossActionState.DashStriking;
            anim.SetBool("isRunning", false);

            // Cảnh báo chớp đỏ trước khi phóng đi
            yield return StartCoroutine(FlashColorRoutine(new Color(1f, 0.2f, 0f), 0.15f));

            float direction = (player != null && player.position.x > transform.position.x) ? 1f : -1f;
            if ((direction > 0 && !facingRight) || (direction < 0 && facingRight))
            {
                Flip();
            }

            PlaySound(flameSlashSound);
            anim.SetTrigger("attack1");

            // Phóng nhanh về phía trước kèm chuỗi bóng mờ
            float dashTime = 0.22f;
            float timer = 0f;
            rb.velocity = new Vector2(direction * 14f, 0);

            while (timer < dashTime)
            {
                timer += Time.deltaTime;
                if (enableGhostAfterimage)
                {
                    SpawnAfterimage(phase2AuraColor, 0.35f);
                }
                yield return new WaitForSeconds(0.04f);
            }

            rb.velocity = Vector2.zero;
            DealMeleeDamage(dashStrikeDamage);
            StartCoroutine(TriggerScreenShake(0.18f, 0.12f));

            yield return new WaitForSeconds(0.25f);
            lastAttackTime = Time.time;
            actionState = BossActionState.Idle;
        }

        // Kỹ năng Lùi Bước Né Đòn (Tactical Backstep)
        private IEnumerator ExecuteTacticalBackstep()
        {
            actionState = BossActionState.Backstepping;
            anim.SetBool("isRunning", false);

            float backwardDir = facingRight ? -1f : 1f;
            rb.velocity = new Vector2(backwardDir * 7.5f, 3.2f);

            for (int i = 0; i < 3; i++)
            {
                if (enableGhostAfterimage)
                {
                    SpawnAfterimage(new Color(0.7f, 0.7f, 1f, 0.5f), 0.3f);
                }
                yield return new WaitForSeconds(0.06f);
            }

            yield return new WaitForSeconds(0.25f);
            rb.velocity = Vector2.zero;

            lastBackstepTime = Time.time;
            actionState = BossActionState.Idle;
        }

        // Kỹ năng Nhảy Bổ Áp Sát (Jump Attack)
        private IEnumerator ExecuteJumpAttack()
        {
            actionState = BossActionState.JumpAttacking;
            anim.SetBool("isRunning", false);

            // Cảnh báo trước khi bật nhảy
            yield return StartCoroutine(FlashColorRoutine(Color.white, 0.12f));
            anim.SetTrigger("jumpAttack");

            float direction = (player != null && player.position.x > transform.position.x) ? 1f : -1f;
            if ((direction > 0 && !facingRight) || (direction < 0 && facingRight))
            {
                Flip();
            }

            float horizontalDistance = player != null ? Mathf.Abs(player.position.x - transform.position.x) : 6f;
            float dynamicForceX = Mathf.Clamp(horizontalDistance / 0.70f, 4.5f, 14f);
            float actualJumpY = jumpForceY > 0 ? jumpForceY : 5.2f;

            rb.velocity = new Vector2(direction * dynamicForceX, actualJumpY);

            float airTimer = 0f;
            bool hitInAir = false;

            yield return new WaitForSeconds(0.12f);

            while (airTimer < 2.5f)
            {
                airTimer += Time.deltaTime;

                if (enableGhostAfterimage && airTimer % 0.08f < 0.04f)
                {
                    SpawnAfterimage(currentPhase == BossPhase.Phase2_FlamingEnrage ? phase2AuraColor : new Color(0.8f, 0.8f, 0.8f, 0.5f), 0.3f);
                }

                // Chém quét trên không nếu lướt qua Musashi
                if (!hitInAir)
                {
                    Collider2D[] airHits = Physics2D.OverlapCircleAll(transform.position + new Vector3(direction * 1.0f, 1.0f, 0), 2.2f);
                    foreach (var h in airHits)
                    {
                        Player target = h.GetComponent<Player>() ?? h.GetComponentInParent<Player>();
                        if (target != null)
                        {
                            target.TakeDamage(jumpAttackDamage * 0.75f);
                            hitInAir = true;
                            break;
                        }
                    }
                }

                // Kiểm tra tiếp đất
                Vector2 checkPos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
                bool isGrounded = Physics2D.OverlapCircle(checkPos, 0.45f, groundLayer) ||
                                  Physics2D.Raycast(checkPos + Vector2.up * 0.2f, Vector2.down, 0.5f, groundLayer);

                if (airTimer > 0.20f && rb.velocity.y <= 0.15f && isGrounded)
                {
                    break;
                }

                yield return null;
            }

            // Tiếp đất chấn động
            PlaySound(jumpLandSound);
            DealJumpSlamDamage();
            StartCoroutine(TriggerScreenShake(0.28f, currentPhase == BossPhase.Phase2_FlamingEnrage ? 0.25f : 0.15f));

            // Ở Phase 2: Kích hoạt sóng chấn động lửa lan tỏa trên mặt sàn
            if (currentPhase == BossPhase.Phase2_FlamingEnrage)
            {
                StartCoroutine(TriggerShockwaveBlast());
            }

            rb.velocity = Vector2.zero;
            yield return new WaitForSeconds(0.35f);

            lastAttackTime = Time.time;
            actionState = BossActionState.Idle;
        }

        // Sóng lửa lan tỏa trên mặt đất (Phase 2 Ground Shockwave)
        private IEnumerator TriggerShockwaveBlast()
        {
            float direction = facingRight ? 1f : -1f;
            Vector3 startPos = transform.position + new Vector3(direction * 1.5f, 0.2f, 0);

            // Sóng lan tỏa trong 3 bước dọc theo sàn
            for (int i = 0; i < 4; i++)
            {
                Vector3 blastPoint = startPos + new Vector3(direction * (i * 1.5f), 0, 0);
                Collider2D[] hits = Physics2D.OverlapCircleAll(blastPoint, 1.4f);

                foreach (var col in hits)
                {
                    Player target = col.GetComponent<Player>() ?? col.GetComponentInParent<Player>();
                    if (target != null)
                    {
                        target.TakeDamage(shockwaveDamage);
                        Debug.Log($"Sóng lửa quét trúng Musashi gây {shockwaveDamage} dame!");
                        break;
                    }
                }

                // Sinh bóng mờ hiệu ứng sóng lửa tại điểm nổ
                if (enableGhostAfterimage)
                {
                    SpawnParticleGlow(blastPoint, new Color(1f, 0.3f, 0f, 0.8f));
                }

                yield return new WaitForSeconds(0.08f);
            }
        }

        private void DealMeleeDamage(float damage)
        {
            Vector3 center = meleeAttackPoint != null ? meleeAttackPoint.position : (transform.position + new Vector3(facingRight ? 1.6f : -1.6f, 1.4f, 0));
            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, meleeAttackRadius);

            bool hitPlayer = false;
            foreach (var col in colliders)
            {
                Player target = col.GetComponent<Player>() ?? col.GetComponentInParent<Player>();
                if (target != null)
                {
                    target.TakeDamage(damage);
                    Debug.Log($"Boss chém trúng Musashi gây {damage} dame!");
                    hitPlayer = true;
                    break;
                }
            }

            // Dự phòng chắc chắn 100% trúng khi Musashi đang đứng im phía trước
            if (!hitPlayer && player != null)
            {
                float hDist = GetHorizontalDistToPlayer();
                float vDist = GetVerticalDistToPlayer();
                bool isAhead = facingRight ? (player.position.x >= transform.position.x - 0.6f) : (player.position.x <= transform.position.x + 0.6f);

                if (isAhead && hDist <= (meleeAttackRange + 0.6f) && vDist <= 2.8f)
                {
                    Player target = player.GetComponent<Player>() ?? player.GetComponentInParent<Player>();
                    if (target != null)
                    {
                        target.TakeDamage(damage);
                        Debug.Log($"Boss chém trúng Musashi (phát hiện đứng im phía trước) gây {damage} dame!");
                    }
                }
            }
        }

        private void DealJumpSlamDamage()
        {
            Vector3 slamCenter = jumpSlamPoint != null ? jumpSlamPoint.position : (transform.position + new Vector3(facingRight ? 1.2f : -1.2f, 0.4f, 0));
            Collider2D[] colliders = Physics2D.OverlapCircleAll(slamCenter, 3.5f);

            bool hitPlayer = false;
            foreach (var col in colliders)
            {
                Player target = col.GetComponent<Player>() ?? col.GetComponentInParent<Player>();
                if (target != null)
                {
                    target.TakeDamage(jumpAttackDamage);
                    Debug.Log($"Boss Jump Attack dộng đất trúng Musashi gây {jumpAttackDamage} dame!");
                    hitPlayer = true;
                    break;
                }
            }

            if (!hitPlayer && player != null)
            {
                float dist = Vector2.Distance(slamCenter, GetPlayerCenter());
                if (dist <= 3.5f)
                {
                    Player target = player.GetComponent<Player>() ?? player.GetComponentInParent<Player>();
                    if (target != null)
                    {
                        target.TakeDamage(jumpAttackDamage);
                        Debug.Log($"Boss Jump Attack dộng đất trúng Musashi (theo cự ly) gây {jumpAttackDamage} dame!");
                    }
                }
            }
        }

        #endregion

        #region --- DEFENSE, COUNTER & ENRAGE TRANSFORMATION ---

        public override void TakeDamage(float incomingDamage)
        {
            if (currentHealth <= 0 || actionState == BossActionState.Dead) return;

            // Bất tử khi đang trong nghi thức biến hình gầm nộ
            if (isInvulnerable)
            {
                return;
            }

            // NẾU ĐANG TRONG TRẠNG THÁI PHÒNG THỦ (Defend) -> CHẶN ĐÒN VÀ PHẢN CÔNG
            if (actionState == BossActionState.Defending)
            {
                PlaySound(defendBlockSound);
                StartCoroutine(FlashColorRoutine(Color.cyan, 0.12f));

                // Triệt tiêu sát thương (chỉ nhận tượng trưng 1 HP)
                currentHealth -= 1f;
                UpdateBossUI();

                // LẬP TỨC PHẢN CÔNG CHỚP NHOÁNG (Counter Attack)
                if (defendCoroutine != null) StopCoroutine(defendCoroutine);
                StartCoroutine(ExecuteCounterAttack());
                return;
            }

            // Tự động kích hoạt phòng thủ giơ kiếm nếu bị chém dồn dập ở Phase 1
            consecutiveHitsTaken++;
            if (currentPhase == BossPhase.Phase1_Normal &&
                consecutiveHitsTaken >= 2 &&
                actionState != BossActionState.Attacking &&
                actionState != BossActionState.JumpAttacking &&
                Random.Range(0, 100) < defendChance)
            {
                consecutiveHitsTaken = 0;
                defendCoroutine = StartCoroutine(DefendRoutine());
                return;
            }

            // Tính sát thương chuẩn
            float actualDamage = incomingDamage - defense;
            if (actualDamage <= 0) actualDamage = 1f;

            currentHealth -= actualDamage;
            UpdateBossUI();

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Super Armor: Ở Phase 2 hoặc khi đang tung chiêu, không bị ngắt đòn Hurt
                if (currentPhase != BossPhase.Phase2_FlamingEnrage && actionState == BossActionState.Idle)
                {
                    anim.SetTrigger("hurt");
                }
                StartCoroutine(FlashColorRoutine(Color.red, 0.12f));
            }
        }

        private IEnumerator DefendRoutine()
        {
            actionState = BossActionState.Defending;
            rb.velocity = Vector2.zero;
            anim.SetTrigger("defend");

            yield return new WaitForSeconds(defendDuration);

            if (actionState == BossActionState.Defending)
            {
                actionState = BossActionState.Idle;
            }
        }

        // Đòn Phản Công Trừng Phạt (Counter Attack khi Musashi chém trúng kiếm thủ)
        private IEnumerator ExecuteCounterAttack()
        {
            actionState = BossActionState.CounterAttacking;
            rb.velocity = Vector2.zero;

            // Flash sáng cảnh báo phản đòn
            yield return StartCoroutine(FlashColorRoutine(Color.yellow, 0.10f));

            anim.SetTrigger("attack1");
            PlaySound(attackSwingSound);

            yield return new WaitForSeconds(0.20f);
            StepForward(3.5f);
            DealMeleeDamage(counterDamage);
            yield return new WaitForSeconds(0.25f);

            actionState = BossActionState.Idle;
        }

        // BIẾN HÌNH NỘ ĐIỆN ẢNH (Enrage Transformation at <= 50% HP)
        private IEnumerator EnrageTransformationRoutine()
        {
            hasTransformed = true;
            currentPhase = BossPhase.Transforming;
            actionState = BossActionState.Shouting;

            isInvulnerable = true;
            rb.velocity = Vector2.zero;

            // Kích hoạt hoạt ảnh gầm nộ Shout (12 frames)
            anim.SetTrigger("shout");
            PlaySound(roarShoutSound);

            // Giai đoạn tích tụ sức mạnh (0.6s)
            yield return new WaitForSeconds(0.5f);

            // BÙNG NỔ NỘ KHÍ (Shout Blast Wave): Đẩy lùi Musashi & Rung chấn màn hình
            StartCoroutine(TriggerScreenShake(0.6f, 0.35f));

            if (player != null)
            {
                float hDist = GetHorizontalDistToPlayer();
                if (hDist <= 6.5f)
                {
                    Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        float knockbackDir = (player.position.x > transform.position.x) ? 1f : -1f;
                        playerRb.velocity = new Vector2(knockbackDir * 9.5f, 4.5f);
                    }
                }
            }

            // Chớp sáng hào quang lửa mãnh liệt
            for (int i = 0; i < 6; i++)
            {
                StartCoroutine(FlashColorRoutine(new Color(1f, 0.35f, 0f), 0.15f));
                if (enableGhostAfterimage)
                {
                    SpawnAfterimage(phase2AuraColor, 0.4f);
                }
                yield return new WaitForSeconds(0.20f);
            }

            yield return new WaitForSeconds(0.3f);

            // CHÍNH THỨC BƯỚC VÀO PHASE 2: HỎA KIẾM THẦN MA
            currentPhase = BossPhase.Phase2_FlamingEnrage;
            anim.SetBool("isEnraged", true);
            moveSpeed = normalMoveSpeed * enragedSpeedMultiplier;
            defense += 10f;
            isInvulnerable = false;
            actionState = BossActionState.Idle;

            // Đổi màu thanh máu sang đỏ rực lửa
            if (bossFillImage != null)
            {
                bossFillImage.color = new Color(1f, 0.25f, 0.05f);
            }

            // Bắt đầu hào quang lửa nhấp nháy liên tục
            passiveFlameAuraCoroutine = StartCoroutine(PassiveFlamePulseRoutine());
        }

        private IEnumerator PassiveFlamePulseRoutine()
        {
            while (currentPhase == BossPhase.Phase2_FlamingEnrage && currentHealth > 0)
            {
                if (bossSpriteRenderer != null)
                {
                    bossSpriteRenderer.color = new Color(1f, 0.85f, 0.75f);
                    yield return new WaitForSeconds(0.3f);
                    bossSpriteRenderer.color = Color.white;
                    yield return new WaitForSeconds(0.3f);
                }
                else
                {
                    yield return null;
                }
            }
        }

        #endregion

        #region --- DEATH & REWARDS ---

        protected override void Die()
        {
            actionState = BossActionState.Dead;
            StopAllCoroutines();

            if (bossSpriteRenderer != null)
            {
                bossSpriteRenderer.color = Color.white;
            }

            TriggerDeathSound();

            anim.SetTrigger("death");
            if (col != null) col.enabled = false;
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0;
            rb.isKinematic = true;

            if (bossUIParent != null)
            {
                bossUIParent.SetActive(false);
            }

            DropBossRewards();

            // Khởi động chuỗi biến mất dứt khoát sau khi gục ngã
            StartCoroutine(DeathDisappearRoutine());
        }

        private IEnumerator DeathDisappearRoutine()
        {
            // Chờ Boss diễn xong trọn vẹn hoạt ảnh ngã gục (1.6s)
            yield return new WaitForSeconds(1.6f);

            // Tắt hoàn toàn hình ảnh của Boss để đảm bảo không còn hình lưu lại trên màn hình
            if (bossSpriteRenderer != null)
            {
                bossSpriteRenderer.enabled = false;
            }

            // Hủy vĩnh viễn GameObject của Boss khỏi Scene
            Destroy(this.gameObject);
        }

        private void DropBossRewards()
        {
            if (coinPrefab == null) return;

            int dropCount = Random.Range(minCoins, maxCoins + 1);
            for (int i = 0; i < dropCount; i++)
            {
                GameObject coin = Instantiate(coinPrefab, transform.position + new Vector3(Random.Range(-1.2f, 1.2f), 1.2f, 0), Quaternion.identity);
                Rigidbody2D coinRb = coin.GetComponent<Rigidbody2D>();
                if (coinRb != null)
                {
                    coinRb.AddForce(new Vector2(Random.Range(-6f, 6f), Random.Range(5f, 11f)), ForceMode2D.Impulse);
                }
            }
        }

        #endregion

        #region --- VISUAL FX: GHOSTING & SCREEN SHAKE ---

        public void SpawnAfterimage(Color color, float lifetime)
        {
            if (bossSpriteRenderer == null || bossSpriteRenderer.sprite == null) return;

            GameObject ghost = new GameObject("BossGhost_" + Time.frameCount);
            ghost.transform.position = transform.position;
            ghost.transform.rotation = transform.rotation;
            ghost.transform.localScale = transform.localScale;

            SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
            sr.sprite = bossSpriteRenderer.sprite;
            sr.sortingLayerID = bossSpriteRenderer.sortingLayerID;
            sr.sortingOrder = bossSpriteRenderer.sortingOrder - 1;
            sr.color = color;

            StartCoroutine(FadeAndDestroyGhost(ghost, sr, color, lifetime));
        }

        private IEnumerator FadeAndDestroyGhost(GameObject ghost, SpriteRenderer sr, Color initialColor, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(initialColor.a, 0f, elapsed / duration);
                if (sr != null)
                {
                    sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
                }
                yield return null;
            }
            if (ghost != null) Destroy(ghost);
        }

        private void SpawnParticleGlow(Vector3 position, Color color)
        {
            GameObject glow = new GameObject("GroundShockGlow");
            glow.transform.position = position;
            glow.transform.localScale = new Vector3(1.5f, 0.4f, 1f);

            SpriteRenderer sr = glow.AddComponent<SpriteRenderer>();
            if (bossSpriteRenderer != null)
            {
                sr.sprite = bossSpriteRenderer.sprite;
                sr.sortingLayerID = bossSpriteRenderer.sortingLayerID;
                sr.sortingOrder = bossSpriteRenderer.sortingOrder + 1;
            }
            sr.color = color;
            StartCoroutine(FadeAndDestroyGhost(glow, sr, color, 0.25f));
        }

        public IEnumerator TriggerScreenShake(float duration, float magnitude)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) yield break;

            Vector3 originalCamPos = mainCam.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                mainCam.transform.localPosition = new Vector3(originalCamPos.x + x, originalCamPos.y + y, originalCamPos.z);
                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCam.transform.localPosition = originalCamPos;
        }

        private IEnumerator FlashColorRoutine(Color targetColor, float duration = 0.12f)
        {
            if (bossSpriteRenderer != null)
            {
                Color prev = bossSpriteRenderer.color;
                bossSpriteRenderer.color = targetColor;
                yield return new WaitForSeconds(duration);
                if (bossSpriteRenderer != null)
                {
                    bossSpriteRenderer.color = (currentPhase == BossPhase.Phase2_FlamingEnrage) ? new Color(1f, 0.85f, 0.75f) : Color.white;
                }
            }
        }

        #endregion

        #region --- HELPERS & UI ---

        private void UpdateBossUI()
        {
            if (bossSliderUI != null)
            {
                bossSliderUI.value = currentHealth / maxHealth;
            }
            if (bossFillImage != null)
            {
                bossFillImage.fillAmount = currentHealth / maxHealth;
            }
            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = currentHealth / maxHealth;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioManager != null && clip != null)
            {
                audioManager.PlayerSFX(clip);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (meleeAttackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(meleeAttackPoint.position, meleeAttackRadius);
            }
            if (jumpSlamPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(jumpSlamPoint.position, jumpSlamRadius);
            }
            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, 0.25f);
            }
        }

        #endregion
    }
}
