using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyBase : MonoBehaviour
{
    [Header("Base Stats")]
    public float maxHealth = 150f;
    public float currentHealth;
    public float defense = 70f;
    public float moveSpeed = 2f;


    protected Animator anim;
    protected Collider2D col; // dùng để tắt va chạm khi chết
    protected Rigidbody2D rb;

    protected Player playerScript;  // script để gọi hàm gây sát thương lên player
    protected Transform player;   // lưu vị trí của player để quái di chuyển theo tấn công
    private SpriteRenderer spriteRenderer;

    [Header("Audio Settings")]
    protected AudioManager audioManager;
    public AudioClip hurtSound;
    public AudioClip deathSound;

    [Header("UI Settings")]
    public Image healthBarFill;


    [Header("Drop Items")]
    public GameObject coinPrefab; // Kéo thả Prefab Coin vào đây trên Inspector
    public int minCoins = 1;      // Rớt tối thiểu mấy xu
    public int maxCoins = 3;      // Rớt tối đa mấy xu

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        audioManager = FindAnyObjectByType<AudioManager>();
        playerScript = FindAnyObjectByType<Player>();
        if (playerScript != null)
        {
            player = playerScript.gameObject.transform;     // tham chiếu đến component transform của player
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
    }



    // hàm này được gọi khi Musashi tấn công enemy (logic áp dụng cho mọi enemy)
    public void TakeDamage(float incomingDamage)
    {
        float actualDamage = incomingDamage - defense; // sát thương trừ vào giáp

        if (actualDamage < 0)    // nếu sát thương bé hơn giáp thì nhận 1 dame
        {
            actualDamage = 1f;
        }

        currentHealth -= actualDamage;      // trừ máu
        Debug.Log(gameObject.name + " mất " + actualDamage + "hp! còn lại: " + currentHealth);

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }

        // KIỂM TRA MÁU TRƯỚC KHI KÍCH HOẠT ANIMATION
        if (currentHealth <= 0)      
        {
            if (healthBarFill != null)
            {
                healthBarFill.transform.parent.gameObject.SetActive(false); // Ẩn BG và Fill
            }
            // Nếu chết: Chỉ gọi Die() (trong Die đã chứa lệnh phát animation death)
            Die();
        }
        else
        {
            // Nếu còn sống: Chỉ chạy animation hurt và phát âm thanh hurt
            anim.SetTrigger("hurt");
            StartCoroutine(FlashRedRoutine());
        }
    }

    // hàm chung nhưng logic có thể bổ sung thêm phần thưởng tùy enemy
    protected virtual void Die()
    {
        TriggerDeathSound();    // phát âm thanh khi bị tiêu diệt
        anim.SetTrigger("death");   // chạy animation death
        col.enabled = false;    // tắt va chạm để đi xuyên xác chết
        this.enabled = false;   // tắt script này
        rb.gravityScale = 0;    // tắt trọng lực để ko rơi xuống
        rb.velocity = Vector2.zero;     // vận tốc về 0 để enemy nằm im ko di chuyển
        DropCoins();
        Destroy(this.gameObject, 2f);   // xóa game object này sau 2s
    }


    protected void TriggerHurtSound()
    {
        if (audioManager != null && hurtSound != null)
        {
            audioManager.PlayerSFX(hurtSound);
        }
    }

    protected void TriggerDeathSound()
    {
        if (audioManager != null && deathSound != null)
        {
            audioManager.PlayerSFX(deathSound);
        }
    }


    private IEnumerator FlashRedRoutine()
    {
        // đổi màu enemy sang đỏ trong 0.1s rồi trả lại màu gốc
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }


    private void DropCoins()
    {
        // Nếu bạn chưa kéo Prefab vào thì thoát hàm để tránh lỗi
        if (coinPrefab == null) return;

        // Random số lượng xu văng ra từ min đến max
        int dropCount = Random.Range(minCoins, maxCoins + 1);

        for (int i = 0; i < dropCount; i++)
        {
            // Sinh ra đồng xu ngay tại vị trí quái vật chết
            GameObject coin = Instantiate(coinPrefab, transform.position, Quaternion.identity);

            // Bổ sung lực nảy vật lý để xu "văng tung tóe"
            Rigidbody2D coinRb = coin.GetComponent<Rigidbody2D>();
            if (coinRb != null)
            {
                // Lực X random trái/phải, lực Y bắn vòng cung lên trời
                float randomForceX = Random.Range(-3f, 3f);
                float randomForceY = Random.Range(4f, 7f);

                // ForceMode2D.Impulse giống như bị búng mạnh 1 phát
                coinRb.AddForce(new Vector2(randomForceX, randomForceY), ForceMode2D.Impulse);
            }
        }
    }

}
