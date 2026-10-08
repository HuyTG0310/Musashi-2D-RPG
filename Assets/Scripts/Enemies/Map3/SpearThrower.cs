using UnityEngine;

namespace Assets.Scripts.Enemies.Map3
{
    // Gắn cùng object với Huntress1 (cùng object có Animator để nhận Animation Event).
    // Huntress1 hỏi CanThrow() để quyết định, rồi bắn trigger "throw".
    // Animation Event ThrowSpearEvent() sẽ tạo spear ở đúng frame buông tay.
    public class SpearThrower : MonoBehaviour
    {
        [Header("Spear Settings")]
        public Spear spearPrefab;
        public Transform spearSpawnPoint;   // object con ở tay cầm spear
        public float spearSpeed = 12f;
        public float spearDamage = 15f;

        [Header("Throw Conditions")]
        public float throwMinRange = 3.5f;  // gần hơn mức này thì không ném (chạy lại đánh cận chiến)
        public float throwMaxRange = 7f;    // nên nhỏ hơn hoặc bằng chaseRange của Huntress1
        public float throwCooldown = 4f;
        public float lastThrowTime = -999f;

        [Header("Sounds")]
        public AudioClip throwSound;

        private AudioManager audioManager;

        private void Start()
        {
            audioManager = FindAnyObjectByType<AudioManager>();
        }

        public bool CanThrow(float distanceToPlayer)
        {
            return spearPrefab != null
                && distanceToPlayer >= throwMinRange
                && distanceToPlayer <= throwMaxRange
                && Time.time >= lastThrowTime + throwCooldown;
        }

        // Huntress1 gọi ngay khi bắt đầu animation ném
        public void MarkThrown()
        {
            lastThrowTime = Time.time;
        }

        // Gọi bằng Animation Event tại frame buông tay của clip throw
        public void ThrowSpearEvent()
        {
            if (spearPrefab == null || spearSpawnPoint == null) return;

            float facing = Mathf.Sign(transform.localScale.x);

            Spear spear = Instantiate(spearPrefab, spearSpawnPoint.position, Quaternion.identity);
            spear.Launch(facing, spearSpeed, spearDamage);

            if (audioManager != null && throwSound != null)
            {
                audioManager.PlayerSFX(throwSound);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, throwMinRange);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, throwMaxRange);
        }
    }
}