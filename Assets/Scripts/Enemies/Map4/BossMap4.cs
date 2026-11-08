using System.Collections;
using TMPro;
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

        [Header("--- PHASE 2 SETTINGS ---")]
        [Range(10, 90)] public float phase2ThresholdPercent = 50f; // Máu tụt xuống <= 50% thì chuyển Phase 2
        public bool refillHealthOnPhase2 = true; // Hồi đầy cây máu lửa khi chuyển sang Phase 2
        public bool testTriggerPhase2Now = false; // Tích chọn trong Inspector để test chuyển Phase 2 ngay
        public bool testTriggerPhase1Now = false; // Tích chọn trong Inspector để test trở về Phase 1

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

        [Header("--- BOSS BGM SETTINGS ---")]
        public AudioClip bossBgmClip;
        public AudioSource bossBgmSource;
        [Range(0f, 1f)] public float bossBgmVolume = 0.8f;

        [Header("--- SKULL HEALTH BAR (RED THEME) ---")]
        public Sprite skullFrameSprite;
        public Sprite skullBgSprite;
        public Sprite skullFillPhase1Sprite; // Bản màu đỏ Ruby (Phase 1)
        public Sprite skullFillPhase2Sprite; // Bản màu lửa bốc cháy (Phase 2)
        public Color healthBarColor = Color.white; // Giữ nguyên đỏ nguyên bản rực rỡ
        public bool showScreenHUDBar = true; // Thanh máu lớn ở đỉnh màn hình (mặc định BẬT)
        public bool showOverheadBar = false; // Ẩn thanh máu nhỏ trên đầu để tập trung vào thanh lớn ở trên

        [Header("--- SCREEN HUD BOSS BAR CUSTOMIZATION ---")]
        public GameObject hudSkullBarPrefab; // Prefab Boss Skull HealthBar (Assets/Prefabs/UI/BossSkullHealthBar.prefab)
        [Range(4f, 14f)] public float hudBarScale = 8.5f; // Độ lớn thanh máu lớn ở đỉnh màn hình (8.5x cực to và hoành tráng)
        public Vector2 hudBarPosition = new Vector2(0f, -40f); // Vị trí trung tâm đỉnh màn hình
        public bool onlyShowWhenPlayerNearby = true; // Chỉ hiện thanh máu khi Musashi tiến vào khu vực Boss
        public float bossTriggerDistance = 14f; // Khoảng cách Musashi bước vào để thanh máu xuất hiện (mặc định 14m)
        public string phase1Subtitle = "GIAI ĐOẠN 1: KIẾM ĐẠO SAMURAI";
        public string phase2Subtitle = "GIAI ĐOẠN 2: HỎA KIẾM THẦN MA (CUỒNG NỘ)";

        [Header("--- RUNTIME SKULL BAR REFS ---")]
        public GameObject overheadSkullBar;
        public Image overheadSkullFill;
        public Image overheadSkullBg;
        public Image overheadSkullFrame;
        public TextMeshProUGUI overheadBossTitle;

        public GameObject hudSkullBar;
        public Image hudSkullFill;
        public Image hudSkullBg;
        public Image hudSkullFrame;
        public TextMeshProUGUI hudBossTitle;
        public TextMeshProUGUI hudBossSubtitle;
        public TextMeshProUGUI hudBossHpText;

        [Header("--- LEGACY UI REFS (BACKWARD COMPATIBLE) ---")]
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

        protected virtual void Awake()
        {
            RemoveOverheadHealthBars();
        }

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

            // Khởi tạo hệ thống thanh máu bản Skull màu đỏ lớn ở đỉnh màn hình
            try
            {
                SetupSkullHealthBar();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BossMap4] SetupSkullHealthBar warning: {ex.Message}");
            }

            if (bossUIParent != null)
            {
                bossUIParent.SetActive(false);
            }

            if (bossNameText != null)
            {
                bossNameText.text = bossName;
            }

            UpdateSkullBarPhaseVisuals();
            UpdateBossUI();
            PlayBossBGM();
        }

        private void OnEnable()
        {
            if (bossSpriteRenderer != null && actionState != BossActionState.Dead && currentHealth > 0)
            {
                PlayBossBGM();
            }
        }

        private void OnDisable()
        {
            StopBossBGM();
        }

        private void PlayBossBGM()
        {
            if (actionState == BossActionState.Dead || currentHealth <= 0) return;

            if (bossBgmClip == null)
            {
                bossBgmClip = Resources.Load<AudioClip>("Audio/Movement Proposition");
            }

            if (bossBgmSource == null)
            {
                bossBgmSource = GetComponent<AudioSource>();
                if (bossBgmSource == null)
                {
                    bossBgmSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (bossBgmClip != null && bossBgmSource != null)
            {
                bossBgmSource.clip = bossBgmClip;
                bossBgmSource.volume = bossBgmVolume;
                bossBgmSource.loop = true;
                bossBgmSource.playOnAwake = true;
                bossBgmSource.spatialBlend = 0f;
                if (!bossBgmSource.isPlaying)
                {
                    bossBgmSource.Play();
                }
            }
        }

        private void StopBossBGM()
        {
            if (bossBgmSource != null && bossBgmSource.isPlaying)
            {
                bossBgmSource.Stop();
            }
        }

        private void Update()
        {
            // Kiểm tra phím tắt hoặc checkbox Inspector để test đổi Phase tức thì
            if (testTriggerPhase2Now || Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                testTriggerPhase2Now = false;
                TriggerPhase2();
            }
            else if (testTriggerPhase1Now || Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                testTriggerPhase1Now = false;
                TriggerPhase1();
            }

            if (currentHealth <= 0 && currentPhase == BossPhase.Phase2_FlamingEnrage)
            {
                return;
            }

            if (player == null || playerScript == null)
            {
                return;
            }

            // Tính toán khoảng cách chuẩn xác theo trọng tâm Boss và Musashi
            float horizontalDistance = GetHorizontalDistToPlayer();
            float verticalDistance = GetVerticalDistToPlayer();
            float distanceToPlayer = Vector2.Distance(GetBossCenter(), GetPlayerCenter());

            // Kiểm tra điều kiện hiển thị thanh máu Boss theo cự ly khu vực Boss
            bool shouldShowHpBar = !onlyShowWhenPlayerNearby || 
                                   (horizontalDistance <= bossTriggerDistance) || 
                                   (currentHealth < maxHealth && horizontalDistance <= bossTriggerDistance + 6f);

            // Bật/tắt thanh máu Screen HUD theo cự ly
            if (hudSkullBar != null && showScreenHUDBar)
            {
                if (shouldShowHpBar && !hudSkullBar.activeSelf && actionState != BossActionState.Dead)
                {
                    hudSkullBar.SetActive(true);
                }
                else if (!shouldShowHpBar && hudSkullBar.activeSelf)
                {
                    hudSkullBar.SetActive(false);
                }
            }

            if (bossUIParent != null)
            {
                if (shouldShowHpBar && !bossUIParent.activeSelf && actionState != BossActionState.Dead)
                {
                    bossUIParent.SetActive(true);
                }
                else if (!shouldShowHpBar && bossUIParent.activeSelf)
                {
                    bossUIParent.SetActive(false);
                }
            }

            // Nếu đang trong trạng thái gầm nộ hoặc chết thì không xử lý AI di chuyển
            if (actionState == BossActionState.Shouting || actionState == BossActionState.Dead)
            {
                return;
            }

            // Kích hoạt biến hình nộ khi máu tụt xuống ngưỡng Phase 2
            if (!hasTransformed && currentPhase == BossPhase.Phase1_Normal)
            {
                float threshold = maxHealth * (phase2ThresholdPercent / 100f);
                if (currentHealth <= threshold)
                {
                    StartCoroutine(EnrageTransformationRoutine());
                    return;
                }
            }

            // Nếu đang bận thi triển kỹ năng thì không xử lý di chuyển AI thông thường
            if (actionState == BossActionState.Attacking ||
                actionState == BossActionState.DashStriking ||
                actionState == BossActionState.Backstepping ||
                actionState == BossActionState.CounterAttacking ||
                actionState == BossActionState.JumpAttacking ||
                actionState == BossActionState.Defending)
            {
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

        private IEnumerator ExecuteMeleeAttackCombo()
        {
            actionState = BossActionState.Attacking;
            rb.velocity = Vector2.zero;
            anim.SetBool("isRunning", false);

            if (currentPhase == BossPhase.Phase1_Normal)
            {
                float roll = Random.value;

                if (roll < 0.35f)
                {
                    yield return StartCoroutine(PerformAttack(1, attack1Damage, 0.35f, 0.35f));
                }
                else if (roll < 0.65f)
                {
                    yield return StartCoroutine(PerformAttack(1, attack1Damage, 0.35f, 0.20f));
                    StepForward(1.5f);
                    yield return StartCoroutine(PerformAttack(2, attack2Damage, 0.30f, 0.35f));
                }
                else
                {
                    yield return StartCoroutine(FlashColorRoutine(new Color(1f, 0.9f, 0.3f), 0.2f));
                    StepForward(2.2f);
                    yield return StartCoroutine(PerformAttack(3, attack3Damage, 0.40f, 0.40f));
                }
            }
            else
            {
                float roll = Random.value;

                if (roll < 0.40f)
                {
                    yield return StartCoroutine(PerformAttack(1, attack1Damage, 0.28f, 0.18f));
                    StepForward(2.0f);
                    yield return StartCoroutine(PerformAttack(2, attack2Damage, 0.25f, 0.25f));
                }
                else if (roll < 0.80f)
                {
                    yield return StartCoroutine(PerformAttack(1, attack1Damage, 0.25f, 0.15f));
                    StepForward(1.8f);
                    yield return StartCoroutine(PerformAttack(2, attack2Damage, 0.25f, 0.15f));
                    StepForward(2.5f);
                    StartCoroutine(TriggerScreenShake(0.2f, 0.15f));
                    yield return StartCoroutine(PerformAttack(3, attack3Damage, 0.35f, 0.35f));
                }
                else
                {
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

        private IEnumerator ExecuteDashStrike()
        {
            actionState = BossActionState.DashStriking;
            anim.SetBool("isRunning", false);

            yield return StartCoroutine(FlashColorRoutine(new Color(1f, 0.2f, 0f), 0.15f));

            float direction = (player != null && player.position.x > transform.position.x) ? 1f : -1f;
            if ((direction > 0 && !facingRight) || (direction < 0 && facingRight))
            {
                Flip();
            }

            PlaySound(flameSlashSound);
            anim.SetTrigger("attack1");

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

        private IEnumerator ExecuteJumpAttack()
        {
            actionState = BossActionState.JumpAttacking;
            anim.SetBool("isRunning", false);

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

                Vector2 checkPos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
                bool isGrounded = Physics2D.OverlapCircle(checkPos, 0.45f, groundLayer) ||
                                  Physics2D.Raycast(checkPos + Vector2.up * 0.2f, Vector2.down, 0.5f, groundLayer);

                if (airTimer > 0.20f && rb.velocity.y <= 0.15f && isGrounded)
                {
                    break;
                }

                yield return null;
            }

            PlaySound(jumpLandSound);
            DealJumpSlamDamage();
            StartCoroutine(TriggerScreenShake(0.28f, currentPhase == BossPhase.Phase2_FlamingEnrage ? 0.25f : 0.15f));

            if (currentPhase == BossPhase.Phase2_FlamingEnrage)
            {
                StartCoroutine(TriggerShockwaveBlast());
            }

            rb.velocity = Vector2.zero;
            yield return new WaitForSeconds(0.35f);

            lastAttackTime = Time.time;
            actionState = BossActionState.Idle;
        }

        private IEnumerator TriggerShockwaveBlast()
        {
            float direction = facingRight ? 1f : -1f;
            Vector3 startPos = transform.position + new Vector3(direction * 1.5f, 0.2f, 0);

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

            if (hudSkullBar != null && !hudSkullBar.activeSelf && showScreenHUDBar)
            {
                hudSkullBar.SetActive(true);
            }

            // NẾU ĐANG TRONG TRẠNG THÁI PHÒNG THỦ (Defend) -> CHẶN ĐÒN VÀ PHẢN CÔNG
            if (actionState == BossActionState.Defending)
            {
                PlaySound(defendBlockSound);
                StartCoroutine(FlashColorRoutine(Color.cyan, 0.12f));

                currentHealth -= 1f;
                UpdateBossUI();

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

            // BẢO VỆ CHỐNG CHẾT ĐỘT TỬ Ở PHASE 1:
            // Khi máu tụt xuống ngưỡng Phase 2 (mặc định 50%), Boss BẮT BUỘC chuyển Phase, không bao giờ bị chết ở Phase 1!
            if (!hasTransformed && currentPhase == BossPhase.Phase1_Normal)
            {
                float threshold = maxHealth * (phase2ThresholdPercent / 100f);
                if (currentHealth - actualDamage <= threshold)
                {
                    currentHealth = threshold;
                    UpdateBossUI();

                    StopAllCoroutines();
                    StartCoroutine(EnrageTransformationRoutine());
                    return;
                }
            }

            currentHealth -= actualDamage;
            UpdateBossUI();

            if (currentHealth <= 0)
            {
                if (!hasTransformed)
                {
                    currentHealth = maxHealth * (phase2ThresholdPercent / 100f);
                    UpdateBossUI();
                    StopAllCoroutines();
                    StartCoroutine(EnrageTransformationRoutine());
                }
                else
                {
                    Die();
                }
            }
            else
            {
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

        private IEnumerator ExecuteCounterAttack()
        {
            actionState = BossActionState.CounterAttacking;
            rb.velocity = Vector2.zero;

            yield return StartCoroutine(FlashColorRoutine(Color.yellow, 0.10f));

            anim.SetTrigger("attack1");
            PlaySound(attackSwingSound);

            yield return new WaitForSeconds(0.20f);
            StepForward(3.5f);
            DealMeleeDamage(counterDamage);
            yield return new WaitForSeconds(0.25f);

            actionState = BossActionState.Idle;
        }

        // BIẾN HÌNH NỘ ĐIỆN ẢNH (Enrage Transformation sang Phase 2)
        public IEnumerator EnrageTransformationRoutine()
        {
            hasTransformed = true;
            currentPhase = BossPhase.Transforming;
            actionState = BossActionState.Shouting;

            isInvulnerable = true;
            rb.velocity = Vector2.zero;

            anim.SetTrigger("shout");
            PlaySound(roarShoutSound);

            yield return new WaitForSeconds(0.5f);

            // BÙNG NỔ NỘ KHÍ (Shout Blast Wave): Đẩy lùi Musashi & Rung chấn màn hình
            StartCoroutine(TriggerScreenShake(0.7f, 0.40f));

            if (player != null)
            {
                float hDist = GetHorizontalDistToPlayer();
                if (hDist <= 7.0f)
                {
                    Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        float knockbackDir = (player.position.x > transform.position.x) ? 1f : -1f;
                        playerRb.velocity = new Vector2(knockbackDir * 10f, 5.0f);
                    }
                }
            }

            // Chuyển đổi giao diện thanh máu sang bản Skull Phase 2 (Đỏ Lửa Rực Rỡ)
            UpdateSkullBarPhaseVisuals();

            // Hiệu ứng hồi máu lửa từ từ đầy lại cây máu đỏ rực
            if (refillHealthOnPhase2)
            {
                float refillDuration = 0.9f;
                float elapsedRefill = 0f;
                float startHp = currentHealth;
                while (elapsedRefill < refillDuration)
                {
                    elapsedRefill += Time.deltaTime;
                    currentHealth = Mathf.Lerp(startHp, maxHealth, elapsedRefill / refillDuration);
                    UpdateBossUI();
                    yield return null;
                }
                currentHealth = maxHealth;
                UpdateBossUI();
            }

            for (int i = 0; i < 5; i++)
            {
                StartCoroutine(FlashColorRoutine(new Color(1f, 0.35f, 0f), 0.15f));
                if (enableGhostAfterimage)
                {
                    SpawnAfterimage(phase2AuraColor, 0.4f);
                }
                yield return new WaitForSeconds(0.18f);
            }

            yield return new WaitForSeconds(0.2f);

            // CHÍNH THỨC BƯỚC VÀO PHASE 2: HỎA KIẾM THẦN MA
            currentPhase = BossPhase.Phase2_FlamingEnrage;
            anim.SetBool("isEnraged", true);
            anim.Play("Boss_Idle_Fire");

            moveSpeed = normalMoveSpeed * enragedSpeedMultiplier;
            defense += 10f;
            isInvulnerable = false;
            actionState = BossActionState.Idle;

            if (passiveFlameAuraCoroutine != null) StopCoroutine(passiveFlameAuraCoroutine);
            passiveFlameAuraCoroutine = StartCoroutine(PassiveFlamePulseRoutine());

            // Đảm bảo cập nhật giao diện Skull Bar & Boss Subtitle sang Phase 2
            UpdateSkullBarPhaseVisuals();

            Debug.Log(">>> BOSS MAP 4 ĐÃ CHÍNH THỨC BƯỚC VÀO PHASE 2 (HỎA KIẾM THẦN MA CUỒNG NỘ)! <<<");
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

        #region --- MANUAL / CONTEXT MENU PHASE CONTROLS (TESTING) ---

        [ContextMenu("Chuyển sang Phase 2 (Hỏa Kiếm Thần Ma)")]
        public void TriggerPhase2()
        {
            if (actionState == BossActionState.Dead) return;
            StopAllCoroutines();
            if (passiveFlameAuraCoroutine != null) StopCoroutine(passiveFlameAuraCoroutine);
            StartCoroutine(EnrageTransformationRoutine());
        }

        [ContextMenu("Chuyển về Phase 1 (Kiếm Đạo Samurai)")]
        public void TriggerPhase1()
        {
            if (actionState == BossActionState.Dead) return;
            StopAllCoroutines();
            if (passiveFlameAuraCoroutine != null) StopCoroutine(passiveFlameAuraCoroutine);

            hasTransformed = false;
            currentPhase = BossPhase.Phase1_Normal;
            actionState = BossActionState.Idle;
            isInvulnerable = false;

            anim.SetBool("isEnraged", false);
            anim.Play("Boss_Idle");

            moveSpeed = normalMoveSpeed;
            defense = 25f;
            currentHealth = maxHealth;

            if (bossSpriteRenderer != null) bossSpriteRenderer.color = Color.white;
            UpdateSkullBarPhaseVisuals();
            UpdateBossUI();

            Debug.Log(">>> Boss Map 4 chuyển về Phase 1 (Kiếm Đạo Samurai) thành công! <<<");
        }

        [ContextMenu("Đổi Phase qua lại (Toggle Phase)")]
        public void TogglePhase()
        {
            if (currentPhase == BossPhase.Phase1_Normal)
            {
                TriggerPhase2();
            }
            else
            {
                TriggerPhase1();
            }
        }

        public void SwitchToPhase(BossPhase targetPhase)
        {
            if (targetPhase == BossPhase.Phase2_FlamingEnrage)
            {
                TriggerPhase2();
            }
            else if (targetPhase == BossPhase.Phase1_Normal)
            {
                TriggerPhase1();
            }
        }

        #endregion

        #region --- DEATH & REWARDS ---

        protected override void Die()
        {
            actionState = BossActionState.Dead;
            StopAllCoroutines();
            StopBossBGM();

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

            if (hudSkullBar != null)
            {
                hudSkullBar.SetActive(false);
            }
            if (bossUIParent != null)
            {
                bossUIParent.SetActive(false);
            }

            DropBossRewards();
            StartCoroutine(DeathDisappearRoutine());

            if (Musashi.UI.GameEndManager.Instance != null)
            {
                Musashi.UI.GameEndManager.Instance.TriggerVictory();
            }
        }

        private IEnumerator DeathDisappearRoutine()
        {
            yield return new WaitForSeconds(1.6f);

            if (bossSpriteRenderer != null)
            {
                bossSpriteRenderer.enabled = false;
            }

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

        #region --- SKULL HEALTH BAR SETUP & UI LOGIC ---

        private void SetupSkullHealthBar()
        {
            // Tự động nạp tài nguyên Sprite Skull nếu chưa được kéo vào Inspector
            if (skullFrameSprite == null)
            {
                skullFrameSprite = Resources.Load<Sprite>("UI/Sprites/SkullHealthbar/skull_frame");
            }
            if (skullBgSprite == null)
            {
                skullBgSprite = Resources.Load<Sprite>("UI/Sprites/SkullHealthbar/skull_bg");
            }
            if (skullFillPhase1Sprite == null)
            {
                skullFillPhase1Sprite = Resources.Load<Sprite>("UI/Sprites/SkullHealthbar/skull_fill");
            }
            if (skullFillPhase2Sprite == null)
            {
                skullFillPhase2Sprite = Resources.Load<Sprite>("UI/Sprites/SkullHealthbar/skull_fill_phase2");
            }

            // 1. Cấu hình thanh máu Skull lớn ở đỉnh trung tâm màn hình (Screen HUD) - ƯU TIÊN SỐ 1
            try
            {
                SetupHudSkullBar();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BossMap4] SetupHudSkullBar error: {ex}");
            }

            // 2. Hủy hoàn toàn mọi thanh máu cũ lơ lửng trên đầu Boss
            RemoveOverheadHealthBars();
        }

        private void RemoveOverheadHealthBars()
        {
            showOverheadBar = false;
            overheadSkullBar = null;
            overheadSkullFill = null;
            overheadSkullBg = null;
            overheadSkullFrame = null;
            overheadBossTitle = null;

            // Quét và xóa sổ vĩnh viễn thanh máu nhỏ trên đầu
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null) continue;
                if (child.name.Contains("HealthBar") || child.GetComponent<Canvas>() != null)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }
        }

        private void SetupHudSkullBar()
        {
            Canvas hudCanvas = null;
            GameObject hudCanvasObj = GameObject.Find("HUDCanvas");
            if (hudCanvasObj != null)
            {
                hudCanvas = hudCanvasObj.GetComponent<Canvas>();
            }
            if (hudCanvas == null)
            {
                HUDManager hudMgr = FindAnyObjectByType<HUDManager>();
                if (hudMgr != null) hudCanvas = hudMgr.GetComponent<Canvas>() ?? hudMgr.GetComponentInParent<Canvas>();
            }
            if (hudCanvas == null)
            {
                Canvas[] allCanvases = FindObjectsOfType<Canvas>();
                foreach (var c in allCanvases)
                {
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.gameObject.name.Contains("HUD") || c.gameObject.name == "Canvas")
                    {
                        hudCanvas = c;
                        break;
                    }
                }
            }

            if (hudCanvas != null && showScreenHUDBar)
            {
                Transform existingHudBar = hudCanvas.transform.Find("BossSkullHUD");
                if (existingHudBar == null && hudSkullBarPrefab != null)
                {
                    GameObject spawned = Instantiate(hudSkullBarPrefab, hudCanvas.transform);
                    spawned.name = "BossSkullHUD";
                    existingHudBar = spawned.transform;
                }
                if (existingHudBar == null)
                {
                    GameObject resPrefab = Resources.Load<GameObject>("UI/BossSkullHealthBar");
                    if (resPrefab != null)
                    {
                        GameObject spawned = Instantiate(resPrefab, hudCanvas.transform);
                        spawned.name = "BossSkullHUD";
                        existingHudBar = spawned.transform;
                    }
                }
                if (existingHudBar == null)
                {
                    GameObject rootHud = new GameObject("BossSkullHUD");
                    rootHud.transform.SetParent(hudCanvas.transform, false);
                    existingHudBar = rootHud.transform;
                }

                hudSkullBar = existingHudBar.gameObject;
                RectTransform rootRect = existingHudBar.GetComponent<RectTransform>() ?? existingHudBar.gameObject.AddComponent<RectTransform>();

                // Đảm bảo nổi trên cùng bằng Canvas sorting riêng
                Canvas barCanvas = existingHudBar.GetComponent<Canvas>();
                if (barCanvas == null) barCanvas = existingHudBar.gameObject.AddComponent<Canvas>();
                barCanvas.overrideSorting = true;
                barCanvas.sortingOrder = 100;

                // Căn chỉnh tuyệt đối ở chính giữa đỉnh màn hình (Top Center)
                rootRect.anchorMin = new Vector2(0.5f, 1f);
                rootRect.anchorMax = new Vector2(0.5f, 1f);
                rootRect.pivot = new Vector2(0.5f, 1f);
                rootRect.anchoredPosition = hudBarPosition;
                rootRect.localScale = Vector3.one;

                // Tính toán kích thước phóng đại theo tỷ lệ pixel art chuẩn xác
                float S = hudBarScale > 0f ? hudBarScale : 8.5f;
                float frameW = 86f * S;
                float frameH = 19f * S;
                float fillW = 67f * S;
                float fillH = 10f * S;
                Vector2 fillOffset = new Vector2(-1.5f * S, -0.5f * S);

                rootRect.sizeDelta = new Vector2(frameW, frameH);

                TMP_FontAsset pixelFont = Resources.Load<TMP_FontAsset>("Fonts/PixelOperator8 SDF");

                // 1. Tên Boss - Căn giữa phía trên thanh máu
                Transform tTrans = existingHudBar.Find("BossTitle");
                if (tTrans == null)
                {
                    GameObject tObj = new GameObject("BossTitle");
                    tObj.transform.SetParent(existingHudBar, false);
                    tTrans = tObj.transform;
                    hudBossTitle = tObj.AddComponent<TextMeshProUGUI>();
                }
                else
                {
                    hudBossTitle = tTrans.GetComponent<TextMeshProUGUI>();
                }
                if (hudBossTitle != null)
                {
                    hudBossTitle.text = bossName;
                    hudBossTitle.fontSize = 24f;
                    hudBossTitle.fontStyle = FontStyles.Bold;
                    hudBossTitle.alignment = TextAlignmentOptions.Center;
                    hudBossTitle.color = new Color(1f, 0.88f, 0.35f);
                    if (pixelFont != null) hudBossTitle.font = pixelFont;
                    hudBossTitle.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                    hudBossTitle.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    hudBossTitle.rectTransform.pivot = new Vector2(0.5f, 0f);
                    hudBossTitle.rectTransform.anchoredPosition = new Vector2(0f, 22f);
                    hudBossTitle.rectTransform.sizeDelta = new Vector2(800f, 36f);
                }

                // 2. Phụ đề Phase - Nằm ngay dưới tên Boss
                Transform subTrans = existingHudBar.Find("BossSubtitle");
                if (subTrans == null)
                {
                    GameObject sObj = new GameObject("BossSubtitle");
                    sObj.transform.SetParent(existingHudBar, false);
                    subTrans = sObj.transform;
                    hudBossSubtitle = sObj.AddComponent<TextMeshProUGUI>();
                }
                else
                {
                    hudBossSubtitle = subTrans.GetComponent<TextMeshProUGUI>();
                }
                if (hudBossSubtitle != null)
                {
                    hudBossSubtitle.text = phase1Subtitle;
                    hudBossSubtitle.fontSize = 13f;
                    hudBossSubtitle.alignment = TextAlignmentOptions.Center;
                    hudBossSubtitle.color = new Color(1f, 0.75f, 0.45f);
                    if (pixelFont != null) hudBossSubtitle.font = pixelFont;
                    hudBossSubtitle.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                    hudBossSubtitle.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    hudBossSubtitle.rectTransform.pivot = new Vector2(0.5f, 0f);
                    hudBossSubtitle.rectTransform.anchoredPosition = new Vector2(0f, 0f);
                    hudBossSubtitle.rectTransform.sizeDelta = new Vector2(800f, 22f);
                }

                // 3. Background - Rãnh tối màu
                Transform bgTrans = existingHudBar.Find("HUD_BG");
                if (bgTrans == null)
                {
                    GameObject bgObj = new GameObject("HUD_BG");
                    bgObj.transform.SetParent(existingHudBar, false);
                    bgTrans = bgObj.transform;
                    hudSkullBg = bgObj.AddComponent<Image>();
                }
                else
                {
                    hudSkullBg = bgTrans.GetComponent<Image>();
                }
                if (hudSkullBg != null)
                {
                    if (skullBgSprite != null) hudSkullBg.sprite = skullBgSprite;
                    hudSkullBg.type = Image.Type.Simple;
                    hudSkullBg.color = Color.white;
                    hudSkullBg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    hudSkullBg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    hudSkullBg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    hudSkullBg.rectTransform.anchoredPosition = fillOffset;
                    hudSkullBg.rectTransform.sizeDelta = new Vector2(fillW, fillH);
                }

                // 4. Fill - Thanh máu đỏ to lớn rực rỡ
                Transform fillTrans = existingHudBar.Find("HUD_Fill");
                if (fillTrans == null)
                {
                    GameObject fillObj = new GameObject("HUD_Fill");
                    fillObj.transform.SetParent(existingHudBar, false);
                    fillTrans = fillObj.transform;
                    hudSkullFill = fillObj.AddComponent<Image>();
                }
                else
                {
                    hudSkullFill = fillTrans.GetComponent<Image>();
                }
                if (hudSkullFill != null)
                {
                    if (skullFillPhase1Sprite != null) hudSkullFill.sprite = skullFillPhase1Sprite;
                    hudSkullFill.type = Image.Type.Filled;
                    hudSkullFill.fillMethod = Image.FillMethod.Horizontal;
                    hudSkullFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                    hudSkullFill.fillAmount = 1f;
                    hudSkullFill.color = healthBarColor;
                    hudSkullFill.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    hudSkullFill.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    hudSkullFill.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    hudSkullFill.rectTransform.anchoredPosition = fillOffset;
                    hudSkullFill.rectTransform.sizeDelta = new Vector2(fillW, fillH);
                }

                // 5. Frame Skull - Khung đầu lâu to lớn đè lên trên
                Transform frameTrans = existingHudBar.Find("HUD_Frame");
                if (frameTrans == null)
                {
                    GameObject frObj = new GameObject("HUD_Frame");
                    frObj.transform.SetParent(existingHudBar, false);
                    frameTrans = frObj.transform;
                    hudSkullFrame = frObj.AddComponent<Image>();
                }
                else
                {
                    hudSkullFrame = frameTrans.GetComponent<Image>();
                }
                if (hudSkullFrame != null)
                {
                    if (skullFrameSprite != null) hudSkullFrame.sprite = skullFrameSprite;
                    hudSkullFrame.type = Image.Type.Simple;
                    hudSkullFrame.color = Color.white;
                    hudSkullFrame.raycastTarget = false;
                    hudSkullFrame.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    hudSkullFrame.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    hudSkullFrame.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    hudSkullFrame.rectTransform.anchoredPosition = Vector2.zero;
                    hudSkullFrame.rectTransform.sizeDelta = new Vector2(frameW, frameH);
                }

                // 6. Số lượng máu HP - Hiển thị ngay giữa thanh máu
                Transform hpNumTrans = existingHudBar.Find("HUD_HpText");
                if (hpNumTrans == null)
                {
                    GameObject hpObj = new GameObject("HUD_HpText");
                    hpObj.transform.SetParent(existingHudBar, false);
                    hpNumTrans = hpObj.transform;
                    hudBossHpText = hpObj.AddComponent<TextMeshProUGUI>();
                }
                else
                {
                    hudBossHpText = hpNumTrans.GetComponent<TextMeshProUGUI>();
                }
                if (hudBossHpText != null)
                {
                    hudBossHpText.fontSize = 15f;
                    hudBossHpText.fontStyle = FontStyles.Bold;
                    hudBossHpText.alignment = TextAlignmentOptions.Center;
                    hudBossHpText.color = Color.white;
                    if (pixelFont != null) hudBossHpText.font = pixelFont;
                    hudBossHpText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    hudBossHpText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    hudBossHpText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    hudBossHpText.rectTransform.anchoredPosition = fillOffset;
                    hudBossHpText.rectTransform.sizeDelta = new Vector2(fillW, 30f);
                }

                if (bgTrans != null)
                {
                    bgTrans.gameObject.SetActive(true);
                    bgTrans.SetSiblingIndex(0);
                }
                if (fillTrans != null)
                {
                    fillTrans.gameObject.SetActive(true);
                    fillTrans.SetSiblingIndex(1);
                }
                if (frameTrans != null)
                {
                    frameTrans.gameObject.SetActive(true);
                    frameTrans.SetSiblingIndex(2);
                }
                if (hpNumTrans != null)
                {
                    hpNumTrans.gameObject.SetActive(true);
                    hpNumTrans.SetSiblingIndex(3);
                }
                if (tTrans != null) tTrans.gameObject.SetActive(true);
                if (subTrans != null) subTrans.gameObject.SetActive(true);

                healthBarFill = hudSkullFill;

                // Mặc định ẩn ban đầu, chỉ hiện khi Musashi tiếp cận khu vực Boss
                hudSkullBar.SetActive(!onlyShowWhenPlayerNearby);
            }
        }

        private void UpdateSkullBarPhaseVisuals()
        {
            bool isPhase2 = currentPhase == BossPhase.Phase2_FlamingEnrage || currentPhase == BossPhase.Transforming;
            Sprite targetFill = isPhase2 ? skullFillPhase2Sprite : skullFillPhase1Sprite;

            if (hudSkullBar != null)
            {
                if (hudSkullFill == null)
                {
                    Transform fillTr = hudSkullBar.transform.Find("HUD_Fill");
                    if (fillTr != null) hudSkullFill = fillTr.GetComponent<Image>();
                }
                if (hudBossSubtitle == null)
                {
                    Transform subTr = hudSkullBar.transform.Find("BossSubtitle");
                    if (subTr != null) hudBossSubtitle = subTr.GetComponent<TextMeshProUGUI>();
                }
            }

            if (hudSkullFill != null && targetFill != null)
            {
                hudSkullFill.sprite = targetFill;
                hudSkullFill.color = healthBarColor;
            }

            if (hudBossSubtitle != null)
            {
                hudBossSubtitle.text = isPhase2 ? phase2Subtitle : phase1Subtitle;
                hudBossSubtitle.color = isPhase2 ? new Color(1f, 0.35f, 0.1f) : new Color(1f, 0.75f, 0.45f);
                hudBossSubtitle.ForceMeshUpdate();
            }

            if (bossFillImage != null)
            {
                bossFillImage.color = isPhase2 ? new Color(1f, 0.25f, 0.05f) : Color.red;
            }
        }

        private void UpdateBossUI()
        {
            float fillRatio = Mathf.Clamp01(currentHealth / maxHealth);

            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = fillRatio;
            }

            if (hudSkullFill != null)
            {
                hudSkullFill.fillAmount = fillRatio;
            }

            if (bossSliderUI != null)
            {
                bossSliderUI.value = fillRatio;
            }

            if (bossFillImage != null)
            {
                bossFillImage.fillAmount = fillRatio;
            }

            if (hudBossHpText != null)
            {
                hudBossHpText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
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
