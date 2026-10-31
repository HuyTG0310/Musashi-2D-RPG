using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Rương kho báu:
///  - Lấp lánh khi chưa mở.
///  - Lại gần hiện "Click E to claim".
///  - Nhấn E: random vật phẩm theo tỷ lệ, cộng vào túi đồ, hiện HÌNH ẢNH vật phẩm bay lên (kèm số lượng nhỏ).
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
        public Sprite icon;                         // hình vật phẩm hiện khi mở rương
        [Min(0)] public int weight = 50;            // tỷ lệ tương đối
        [Min(1)] public int minAmount = 1;
        [Min(1)] public int maxAmount = 1;
        public Color color = Color.white;           // màu dự phòng nếu chưa gán icon
    }

    [Header("Bảng vật phẩm (tỷ lệ = weight / tổng weight)")]
    public List<LootEntry> lootTable = new List<LootEntry>
    {
        new LootEntry { displayName = "Coin",     type = ItemType.Coin,     weight = 50, minAmount = 5, maxAmount = 15, color = new Color(1f, 0.85f, 0.2f) },
        new LootEntry { displayName = "Apple",    type = ItemType.Apple,    weight = 25, minAmount = 1, maxAmount = 2,  color = new Color(1f, 0.4f, 0.4f) },
        new LootEntry { displayName = "Shuriken", type = ItemType.Shuriken, weight = 15, minAmount = 2, maxAmount = 5,  color = new Color(0.7f, 0.85f, 1f) },
        new LootEntry { displayName = "Sake",     type = ItemType.Sake,     weight = 10, minAmount = 1, maxAmount = 1,  color = new Color(0.6f, 1f, 0.7f) },
    };

    [Header("Số lần quay thưởng mỗi rương")]
    [Min(1)] public int minRolls = 2;
    [Min(1)] public int maxRolls = 3;

    [Header("Cài đặt")]
    public KeyCode interactKey = KeyCode.E;
    public bool destroyAfterOpen = false;

    [Header("Hình ảnh rương")]
    public Animator anim;                           // (tuỳ chọn) có trigger "Open"
    public SpriteRenderer spriteRenderer;
    public Sprite closedSprite;
    public Sprite openedSprite;

    [Header("Âm thanh")]
    public AudioClip openSound;

    [Header("Hiệu ứng lấp lánh")]
    public bool sparkle = true;
    [Min(1)] public int sparkleCount = 6;
    public Color sparkleColor = new Color(1f, 0.95f, 0.6f);
    public float sparkleScale = 1f;                 // 1 = mỗi pixel của sao bằng đúng 1 pixel của rương
    public Vector2 sparkleLifetime = new Vector2(0.7f, 1.3f);
    public bool sparkleAfterOpen = true;            // vẫn lấp lánh sau khi đã mở rương
    public bool burstOnOpen = true;                 // bắn một đợt sao khi mở rương

    [Header("Gợi ý tương tác")]
    public GameObject promptUI;                     // (tuỳ chọn) tự gán UI riêng; để trống = tự tạo chữ
    public string promptText = "Click {key} to claim";
    public float promptSize = 4f;
    public float promptOffset = 0.5f;               // cao hơn đỉnh rương bao nhiêu
    public TMP_FontAsset promptFont;                // (tuỳ chọn) để trống = font mặc định

    [Header("Popup vật phẩm khi mở")]
    public float iconSize = 0.9f;                   // cỡ hình vật phẩm (đơn vị Unity)
    public float iconSpacing = 1.2f;
    public bool showAmount = true;                  // hiện số lượng nhỏ "x12" dưới hình
    public float amountSize = 3f;
    public float floatHeight = 1.3f;
    public float floatDuration = 1.8f;

    [Header("Sorting (nếu bị che bởi background)")]
    public int uiSortingOrder = 1000;               // cho chữ và popup
    public string popupSortingLayer = "";           // để trống = dùng Sorting Layer của rương

    // ---------- trạng thái ----------
    private bool isPlayerInRange = false;
    private bool isOpened = false;
    private Player playerScript;

    private TextMeshPro promptTMP;
    private float promptBaseY;

    private class Spark
    {
        public SpriteRenderer sr;
        public Vector3 pos, vel;
        public float t, life;
    }
    private readonly List<Spark> sparks = new List<Spark>();
    private GameObject sparkleRoot;

    private Sprite[] starSprites;
    private static Material unlitMat;
    private static Sprite dotSprite;

    // =====================================================
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && closedSprite == null) closedSprite = spriteRenderer.sprite;
        SetVisualState(false);

        if (promptUI != null) promptUI.SetActive(false);
        else CreatePrompt();

        if (sparkle) CreateSparkles();

        if (anim == null && openedSprite == null)
            Debug.LogWarning("Chest: chưa gán Opened Sprite hoặc Animator nên rương sẽ không đổi hình!", this);
    }

    private void OnDestroy()
    {
        if (promptTMP != null) Destroy(promptTMP.gameObject);
        if (sparkleRoot != null) Destroy(sparkleRoot);
    }

    private void Update()
    {
        UpdateSparkles();
        UpdatePrompt();

        if (isOpened || !isPlayerInRange) return;
        if (Time.timeScale == 0f) return;

        if (Input.GetKeyDown(interactKey)) Open();
    }

    // =====================================================
    // MỞ RƯƠNG
    // =====================================================
    public void Open()
    {
        if (isOpened || playerScript == null) return;
        isOpened = true;

        Dictionary<LootEntry, int> loot = RollLoot();
        foreach (var kv in loot) GiveToPlayer(kv.Key.type, kv.Value);

        SetVisualState(true);
        if (openSound != null) FindAnyObjectByType<AudioManager>()?.PlayerSFX(openSound);

        if (!sparkleAfterOpen) StopAmbientSparkles();
        if (sparkle && burstOnOpen) StartCoroutine(BurstRoutine());

        ShowLootPopup(loot);

        if (destroyAfterOpen) StartCoroutine(DestroyDelayed(floatDuration + 0.2f));
    }

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

    // =====================================================
    // GỢI Ý "Click E to claim"
    // =====================================================
    private void CreatePrompt()
    {
        var go = new GameObject("ChestPrompt");
        Bounds b = GetBounds();
        go.transform.position = new Vector3(b.center.x, b.max.y + promptOffset, transform.position.z);
        promptBaseY = go.transform.position.y;

        promptTMP = go.AddComponent<TextMeshPro>();
        if (promptFont != null) promptTMP.font = promptFont;
        promptTMP.text = promptText.Replace("{key}", interactKey.ToString());
        promptTMP.fontSize = promptSize;
        promptTMP.alignment = TextAlignmentOptions.Center;
        promptTMP.fontStyle = FontStyles.Bold;
        promptTMP.outlineWidth = 0.25f;
        promptTMP.outlineColor = Color.black;
        ApplyUISorting(promptTMP);
        go.SetActive(false);
    }

    private void UpdatePrompt()
    {
        bool show = isPlayerInRange && !isOpened;

        if (promptUI != null)
        {
            if (promptUI.activeSelf != show) promptUI.SetActive(show);
            return;
        }
        if (promptTMP == null) return;

        if (promptTMP.gameObject.activeSelf != show) promptTMP.gameObject.SetActive(show);
        if (show)
        {
            // nhún nhẹ lên xuống cho dễ để ý
            Vector3 p = promptTMP.transform.position;
            p.y = promptBaseY + Mathf.Sin(Time.time * 3f) * 0.05f;
            promptTMP.transform.position = p;
        }
    }

    // =====================================================
    // LẤP LÁNH (pixel art, bám theo lưới pixel của rương nên không bị vỡ hình)
    // =====================================================
    private float ChestPPU()
    {
        return (spriteRenderer != null && spriteRenderer.sprite != null) ? spriteRenderer.sprite.pixelsPerUnit : 16f;
    }

    // kích thước 1 pixel của rương ngoài thế giới game
    private float PixelWorld()
    {
        return Mathf.Abs(transform.lossyScale.x) / ChestPPU();
    }

    // làm tròn vị trí vào lưới pixel của rương để sao luôn sắc nét
    private Vector3 Snap(Vector3 p)
    {
        float px = PixelWorld();
        if (px <= 0f) return p;
        Vector3 o = transform.position;
        p.x = o.x + (Mathf.Round((p.x - o.x) / px - 0.5f) + 0.5f) * px;
        p.y = o.y + (Mathf.Round((p.y - o.y) / px - 0.5f) + 0.5f) * px;
        return p;
    }

    private void BuildStarSprites()
    {
        if (starSprites != null) return;
        float ppu = ChestPPU();
        starSprites = new Sprite[] { MakeStar(3, ppu), MakeStar(5, ppu), MakeStar(9, ppu) }; // nhỏ, vừa, to
    }

    // ngôi sao 4 cánh n x n (n lẻ), Point filter, không làm mờ
    private static Sprite MakeStar(int n, float ppu)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var clear = new Color(1, 1, 1, 0);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) tex.SetPixel(x, y, clear);
        int c = n / 2;
        tex.SetPixel(c, c, Color.white);
        for (int d = 1; d <= c; d++)
        {
            float a = d < c - 1 ? 1f : (d == c - 1 ? 0.8f : 0.5f);   // đầu cánh nhạt dần
            var col = new Color(1, 1, 1, a);
            tex.SetPixel(c + d, c, col); tex.SetPixel(c - d, c, col);
            tex.SetPixel(c, c + d, col); tex.SetPixel(c, c - d, col);
        }
        if (n >= 9)
        {
            var dg = new Color(1, 1, 1, 0.5f);
            tex.SetPixel(c + 1, c + 1, dg); tex.SetPixel(c - 1, c + 1, dg);
            tex.SetPixel(c + 1, c - 1, dg); tex.SetPixel(c - 1, c - 1, dg);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), ppu);
    }

    private void CreateSparkles()
    {
        BuildStarSprites();
        sparkleRoot = new GameObject("ChestSparkles");
        float sc = Mathf.Abs(transform.lossyScale.x) * sparkleScale;
        for (int i = 0; i < sparkleCount; i++)
        {
            var s = new Spark();
            s.sr = MakeSprite("Sparkle", starSprites[0], sparkleRoot.transform, 5);
            s.sr.color = sparkleColor;
            s.sr.transform.localScale = Vector3.one * sc;
            Respawn(s);
            s.t = -Random.Range(0f, s.life);       // lệch pha để không nhấp nháy cùng lúc
            sparks.Add(s);
        }
    }

    private void Respawn(Spark s)
    {
        Bounds b = GetBounds();
        float x = Random.Range(b.min.x + b.size.x * 0.1f, b.max.x - b.size.x * 0.1f);
        float y = Random.Range(b.min.y + b.size.y * 0.2f, b.max.y);
        s.pos = new Vector3(x, y, transform.position.z);
        s.sr.transform.position = Snap(s.pos);
        s.life = Random.Range(sparkleLifetime.x, sparkleLifetime.y);
        s.vel = new Vector3(0f, Random.Range(0.1f, 0.3f), 0f);
        s.t = 0f;
    }

    private void UpdateSparkles()
    {
        if (starSprites == null) return;
        foreach (var s in sparks)
        {
            if (s.sr == null) continue;
            s.t += Time.deltaTime;
            if (s.t < 0f) { s.sr.enabled = false; continue; }
            s.sr.enabled = true;

            float k = s.t / s.life;
            if (k >= 1f) { Respawn(s); continue; }

            float pulse = Mathf.Sin(k * Mathf.PI);                         // 0 -> 1 -> 0
            s.sr.sprite = starSprites[pulse < 0.34f ? 0 : (pulse < 0.7f ? 1 : 2)];   // đổi cỡ theo bậc, không co giãn lẻ
            s.pos += s.vel * Time.deltaTime;
            s.sr.transform.position = Snap(s.pos);
            s.sr.color = sparkleColor;
        }
    }

    private void StopAmbientSparkles()
    {
        if (sparkleRoot != null) Destroy(sparkleRoot);
        sparks.Clear();
    }

    private IEnumerator BurstRoutine()
    {
        BuildStarSprites();
        Bounds b = GetBounds();
        var list = new List<Spark>();
        var root = new GameObject("ChestBurst");
        float sc = Mathf.Abs(transform.lossyScale.x) * sparkleScale;
        for (int i = 0; i < 14; i++)
        {
            var s = new Spark();
            s.sr = MakeSprite("BurstSpark", starSprites[2], root.transform, 6);
            s.sr.color = sparkleColor;
            s.sr.transform.localScale = Vector3.one * sc;
            s.pos = new Vector3(b.center.x, b.center.y, transform.position.z);
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float spd = Random.Range(1.5f, 3.5f);
            s.vel = new Vector3(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + 1f, 0f);
            s.life = Random.Range(0.45f, 0.8f);
            list.Add(s);
        }

        float t = 0f, dur = 0.8f;
        while (t < dur)
        {
            float dt = Time.deltaTime;
            t += dt;
            foreach (var s in list)
            {
                if (s.sr == null) continue;
                s.t += dt;
                float k = s.t / s.life;
                if (k >= 1f) { s.sr.enabled = false; continue; }
                s.vel.y -= 4f * dt;                                       // rơi nhẹ
                s.pos += s.vel * dt;
                s.sr.transform.position = Snap(s.pos);
                s.sr.sprite = starSprites[k < 0.5f ? 2 : (k < 0.8f ? 1 : 0)];   // nhỏ dần
            }
            yield return null;
        }
        Destroy(root);
    }

    // =====================================================
    // POPUP HÌNH ẢNH VẬT PHẨM
    // =====================================================
    private void ShowLootPopup(Dictionary<LootEntry, int> loot)
    {
        if (loot.Count == 0) return;
        Debug.Log($"Chest: hiện popup {loot.Count} loại vật phẩm", this);

        Bounds b = GetBounds();
        Vector3 start = new Vector3(b.center.x, b.max.y + 0.8f, transform.position.z);
        var root = new GameObject("ChestLootPopup");
        root.transform.position = start;

        var renderers = new List<SpriteRenderer>();
        var texts = new List<TextMeshPro>();

        float startX = -(loot.Count - 1) * iconSpacing * 0.5f;
        int i = 0;
        foreach (var kv in loot)
        {
            LootEntry e = kv.Key;
            Vector3 slot = new Vector3(startX + i * iconSpacing, 0f, 0f);

            var icon = MakeSprite("Icon", e.icon != null ? e.icon : GetDotSprite(), root.transform, 0);
            ApplyUISorting(icon);
            if (e.icon == null) Debug.LogWarning($"Chest: '{e.displayName}' chưa gán Icon trong Loot Table, đang dùng chấm màu thay thế.", this);
            icon.color = e.icon != null ? Color.white : e.color;
            icon.transform.localPosition = slot;

            // co hình về đúng cỡ iconSize
            Vector2 size = icon.sprite.bounds.size;
            float s = iconSize / Mathf.Max(size.x, size.y, 0.0001f);
            icon.transform.localScale = Vector3.one * s;
            renderers.Add(icon);

            if (showAmount)
            {
                var go = new GameObject("Amount");
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = slot + new Vector3(0f, -iconSize * 0.75f, 0f);
                var tmp = go.AddComponent<TextMeshPro>();
                if (promptFont != null) tmp.font = promptFont;
                tmp.text = "x" + kv.Value;
                tmp.fontSize = amountSize;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Bold;
                tmp.outlineWidth = 0.25f;
                tmp.outlineColor = Color.black;
                ApplyUISorting(tmp);
                texts.Add(tmp);
            }
            i++;
        }

        StartCoroutine(PopupRoutine(root, start, renderers, texts));
    }

    private IEnumerator PopupRoutine(GameObject root, Vector3 start, List<SpriteRenderer> icons, List<TextMeshPro> texts)
    {
        float t = 0f, pop = 0.18f;
        while (t < floatDuration && root != null)
        {
            t += Time.deltaTime;
            float k = t / floatDuration;

            // bật to ra có độ nảy nhẹ
            float scale = t < pop ? Mathf.LerpUnclamped(0.2f, 1f, EaseOutBack(t / pop)) : 1f;
            root.transform.localScale = Vector3.one * scale;

            // bay lên chậm dần
            float rise = 1f - (1f - k) * (1f - k);
            root.transform.position = start + Vector3.up * (floatHeight * rise);

            // mờ dần ở 40% cuối
            float a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            foreach (var sr in icons) if (sr != null) { var c = sr.color; c.a = a; sr.color = c; }
            foreach (var tx in texts) if (tx != null) tx.alpha = a;
            yield return null;
        }
        if (root != null) Destroy(root);
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        x = Mathf.Clamp01(x);
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    // =====================================================
    // TIỆN ÍCH
    // =====================================================
    private Bounds GetBounds()
    {
        if (spriteRenderer != null) return spriteRenderer.bounds;
        var col = GetComponent<Collider2D>();
        return col != null ? col.bounds : new Bounds(transform.position, Vector3.one);
    }

    private SpriteRenderer MakeSprite(string objName, Sprite sprite, Transform parent, int orderAbove)
    {
        var go = new GameObject(objName);
        if (parent != null) go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        var mat = GetUnlitMaterial();
        if (mat != null) sr.sharedMaterial = mat;      // không phụ thuộc ánh sáng 2D nên luôn nhìn rõ
        if (spriteRenderer != null)
        {
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder = spriteRenderer.sortingOrder + orderAbove;
        }
        else sr.sortingOrder = 100 + orderAbove;
        return sr;
    }

    private int UILayerID()
    {
        if (!string.IsNullOrEmpty(popupSortingLayer)) return SortingLayer.NameToID(popupSortingLayer);
        return spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
    }

    private void ApplyUISorting(TextMeshPro tmp)
    {
        tmp.sortingLayerID = UILayerID();
        tmp.sortingOrder = uiSortingOrder;
    }

    private void ApplyUISorting(SpriteRenderer sr)
    {
        sr.sortingLayerID = UILayerID();
        sr.sortingOrder = uiSortingOrder;
    }

    private static Material GetUnlitMaterial()
    {
        if (unlitMat == null)
        {
            var sh = Shader.Find("Sprites/Default");
            if (sh != null) unlitMat = new Material(sh);
        }
        return unlitMat;
    }

    // chấm tròn dự phòng khi chưa gán icon
    private static Sprite GetDotSprite()
    {
        if (dotSprite != null) return dotSprite;
        const int n = 16;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - 7.5f, dy = y - 7.5f;
                tex.SetPixel(x, y, dx * dx + dy * dy <= 49f ? Color.white : new Color(1, 1, 1, 0));
            }
        tex.Apply();
        dotSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return dotSprite;
    }

    private IEnumerator DestroyDelayed(float delay)
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        GetComponent<Collider2D>().enabled = false;
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    // =====================================================
    // TRIGGER
    // =====================================================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        isPlayerInRange = true;
        playerScript = collision.GetComponent<Player>();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        isPlayerInRange = false;
        playerScript = null;
    }
}