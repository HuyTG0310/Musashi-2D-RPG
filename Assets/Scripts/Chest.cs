using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Rương kho báu: nhấn E để mở. Vật phẩm được random theo tỷ lệ (weight),
/// cộng thẳng vào túi đồ Player và hiện thông báo nổi lên trên rương.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Chest : MonoBehaviour
{
    public enum ItemType { Coin, Apple, Shuriken, Sake }

    [System.Serializable]
    public class LootEntry
    {
        public string displayName = "Coin";
        public ItemType type = ItemType.Coin;
        [Min(0)] public int weight = 50;          // tỷ lệ tương đối, không cần cộng đủ 100
        [Min(1)] public int minAmount = 1;
        [Min(1)] public int maxAmount = 1;
        public Color color = Color.white;         // màu chữ thông báo
    }

    [Header("Bảng vật phẩm (tỷ lệ = weight / tổng weight)")]
    public List<LootEntry> lootTable = new List<LootEntry>
    {
        new LootEntry { displayName = "Coin",       type = ItemType.Coin,     weight = 50, minAmount = 5, maxAmount = 15, color = new Color(1f, 0.85f, 0.2f) },
        new LootEntry { displayName = "Apple",      type = ItemType.Apple,    weight = 25, minAmount = 1, maxAmount = 2,  color = new Color(1f, 0.4f, 0.4f) },
        new LootEntry { displayName = "Shuriken", type = ItemType.Shuriken, weight = 15, minAmount = 2, maxAmount = 5,  color = new Color(0.7f, 0.85f, 1f) },
        new LootEntry { displayName = "Sake",     type = ItemType.Sake,     weight = 10, minAmount = 1, maxAmount = 1,  color = new Color(0.6f, 1f, 0.7f) },
    };

    [Header("Số lần quay thưởng mỗi rương")]
    [Min(1)] public int minRolls = 2;
    [Min(1)] public int maxRolls = 3;

    [Header("Cài đặt")]
    public KeyCode interactKey = KeyCode.E;
    public bool destroyAfterOpen = false;

    [Header("Hình ảnh")]
    public Animator anim;                   // (tuỳ chọn) có trigger "Open"
    public SpriteRenderer spriteRenderer;
    public Sprite closedSprite;
    public Sprite openedSprite;
    public GameObject promptUI;             // (tuỳ chọn) chữ "Nhấn E"

    [Header("Âm thanh")]
    public AudioClip openSound;

    [Header("Thông báo nổi trên rương")]
    public TMP_FontAsset notificationFont;  // gán font có dấu tiếng Việt nếu chữ bị lỗi ô vuông
    public float notificationSize = 5f;
    public string textSortingLayer = "";    // để trống = dùng cùng Sorting Layer với rương
    public int textSortingOrder = 1000;     // càng cao càng nằm trên cùng
    public float floatHeight = 1.2f;
    public float floatDuration = 1.8f;

    [Header("Thông báo trên màn hình (tuỳ chọn)")]
    public TextMeshProUGUI notificationText;
    public float notificationTime = 2f;

    private bool isPlayerInRange = false;
    private bool isOpened = false;
    private Player playerScript;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && closedSprite == null) closedSprite = spriteRenderer.sprite;
        if (notificationFont == null && notificationText != null) notificationFont = notificationText.font;
        SetVisualState(false);

        if (promptUI != null) promptUI.SetActive(false);
        if (notificationText != null) notificationText.gameObject.SetActive(false);

        if (anim == null && openedSprite == null)
            Debug.LogWarning("Chest: chưa gán Opened Sprite hoặc Animator nên rương sẽ không đổi hình!", this);
    }

    private void Update()
    {
        if (isOpened || !isPlayerInRange) return;
        if (Time.timeScale == 0f) return;

        if (Input.GetKeyDown(interactKey)) Open();
    }

    public void Open()
    {
        if (isOpened || playerScript == null) return;
        isOpened = true;

        // 1. random vật phẩm theo tỷ lệ
        Dictionary<LootEntry, int> loot = RollLoot();

        // 2. cộng vào túi đồ Player
        foreach (var kv in loot) GiveToPlayer(kv.Key.type, kv.Value);

        // 3. hình ảnh / âm thanh
        SetVisualState(true);
        if (promptUI != null) promptUI.SetActive(false);
        if (openSound != null) FindAnyObjectByType<AudioManager>()?.PlayerSFX(openSound);

        // 4. thông báo
        ShowFloatingText(loot);
        if (notificationText != null) ShowScreenNotification(BuildLootMessage(loot));

        if (destroyAfterOpen) StartCoroutine(DestroyDelayed(floatDuration + 0.2f));
    }

    // ---------- Loot ----------
    private Dictionary<LootEntry, int> RollLoot()
    {
        var result = new Dictionary<LootEntry, int>();

        int totalWeight = 0;
        foreach (var e in lootTable) totalWeight += Mathf.Max(0, e.weight);
        if (totalWeight <= 0) return result;

        int rolls = Random.Range(minRolls, Mathf.Max(minRolls, maxRolls) + 1);
        for (int i = 0; i < rolls; i++)
        {
            int r = Random.Range(0, totalWeight);
            foreach (var e in lootTable)
            {
                r -= Mathf.Max(0, e.weight);
                if (r < 0)
                {
                    int amount = Random.Range(e.minAmount, Mathf.Max(e.minAmount, e.maxAmount) + 1);
                    result[e] = result.TryGetValue(e, out int old) ? old + amount : amount;
                    break;
                }
            }
        }
        return result;
    }

    private void GiveToPlayer(ItemType type, int amount)
    {
        switch (type)
        {
            case ItemType.Coin: playerScript.coins += amount; break;
            case ItemType.Apple: playerScript.appleCount += amount; break;
            case ItemType.Shuriken: playerScript.shurikenCount += amount; break;
            case ItemType.Sake: playerScript.sakeCount += amount; break;
        }
    }

    // ---------- Visual ----------
    private void SetVisualState(bool open)
    {
        if (anim != null)
        {
            if (open) anim.SetTrigger("Open");
        }
        else if (spriteRenderer != null)
        {
            Sprite target = open ? openedSprite : closedSprite;
            if (target != null) spriteRenderer.sprite = target;
        }
    }

    // ---------- Thông báo ----------
    private string BuildLootMessage(Dictionary<LootEntry, int> loot)
    {
        if (loot.Count == 0) return "Empty chest!";
        var parts = new List<string>();
        foreach (var kv in loot) parts.Add($"{kv.Value} {kv.Key.displayName}");
        return "Received: " + string.Join(" | ", parts);
    }

    private void ShowFloatingText(Dictionary<LootEntry, int> loot)
    {
        // chữ nổi trong thế giới game, mỗi vật phẩm một dòng, có màu riêng
        string text = "";
        if (loot.Count == 0) text = "Empty chest!";
        foreach (var kv in loot)
        {
            string hex = ColorUtility.ToHtmlStringRGB(kv.Key.color);
            text += $"<color=#{hex}>+{kv.Value} {kv.Key.displayName}</color>\n";
        }

        var go = new GameObject("ChestLootText");
        go.transform.position = transform.position + Vector3.up * 0.8f;

        var tmp = go.AddComponent<TextMeshPro>();
        if (notificationFont != null) tmp.font = notificationFont;
        tmp.text = text.TrimEnd('\n');
        tmp.fontSize = notificationSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;

        // đưa chữ lên trên cùng: cùng Sorting Layer với rương (hoặc layer chỉ định) + Order rất cao
        if (!string.IsNullOrEmpty(textSortingLayer))
            tmp.sortingLayerID = SortingLayer.NameToID(textSortingLayer);
        else if (spriteRenderer != null)
            tmp.sortingLayerID = spriteRenderer.sortingLayerID;
        tmp.sortingOrder = textSortingOrder;
        go.layer = gameObject.layer;
        go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, transform.position.z);

        StartCoroutine(FloatAndFade(tmp));
    }

    private IEnumerator FloatAndFade(TextMeshPro tmp)
    {
        Vector3 start = tmp.transform.position;
        float t = 0f;
        while (t < floatDuration && tmp != null)
        {
            t += Time.deltaTime;
            float k = t / floatDuration;
            tmp.transform.position = start + Vector3.up * (floatHeight * k);
            // giữ rõ 60% thời gian đầu rồi mờ dần
            tmp.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            yield return null;
        }
        if (tmp != null) Destroy(tmp.gameObject);
    }

    private void ShowScreenNotification(string message)
    {
        StopCoroutine(nameof(HideNotification));
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        StartCoroutine(nameof(HideNotification));
    }

    private IEnumerator HideNotification()
    {
        yield return new WaitForSeconds(notificationTime);
        if (notificationText != null) notificationText.gameObject.SetActive(false);
    }

    private IEnumerator DestroyDelayed(float delay)
    {
        // ẩn rương ngay nhưng chờ chữ thông báo bay xong mới huỷ
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        GetComponent<Collider2D>().enabled = false;
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    // ---------- Trigger ----------
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        isPlayerInRange = true;
        playerScript = collision.GetComponent<Player>();
        if (!isOpened && promptUI != null) promptUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        isPlayerInRange = false;
        playerScript = null;
        if (promptUI != null) promptUI.SetActive(false);
    }
}