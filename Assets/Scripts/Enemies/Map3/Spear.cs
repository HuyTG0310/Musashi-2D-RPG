using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Spear : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float lifeTime = 5f;          // tự hủy nếu bay mãi
    [SerializeField] private float stuckLifeTime = 3f;     // cắm xuống đất bao lâu thì biến mất

    [Header("Animation (tùy chọn)")]
    [SerializeField] private bool useHitAnimation = false; // bật nếu spear có clip "hit" (trigger "hit")
    [SerializeField] private float hitAnimDuration = 0.4f; // độ dài clip hit (giây), chờ xong mới xóa

    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D col;
    private float damage;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
    }

    // Bay ngang theo hướng nhìn của Huntress (facing > 0: sang phải, < 0: sang trái)
    public void Launch(float facing, float speed, float dmg)
    {
        damage = dmg;
        float dir = Mathf.Sign(facing);

        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        rb.velocity = new Vector2(dir * speed, 0f);

        // Lật sprite bằng scale thay vì xoay (xoay 180° sẽ làm sprite bị lộn ngược)
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        transform.localScale = scale;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // Trúng player
        Player target = other.GetComponent<Player>();
        if (target == null) target = other.GetComponentInParent<Player>();
        if (target != null)
        {
            hasHit = true;
            target.TakeDamage(damage);   // <-- đổi tên hàm cho khớp script Player của bạn
            StopSpear();
            PlayHit();
            Destroy(gameObject, useHitAnimation ? hitAnimDuration : 0f);
            return;
        }

        // Trúng đất/tường -> cắm lại
        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            hasHit = true;
            StopSpear();
            PlayHit();
            Destroy(gameObject, stuckLifeTime);
        }
    }

    private void StopSpear()
    {
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        col.enabled = false;
    }

    private void PlayHit()
    {
        if (useHitAnimation && anim != null)
        {
            anim.SetTrigger("hit");
        }
    }
}