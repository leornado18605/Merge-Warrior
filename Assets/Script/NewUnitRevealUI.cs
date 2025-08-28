using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewUnitRevealUI : MonoBehaviour
{
    public static NewUnitRevealUI Instance { get; private set; }

    // ===== Root =====
    [Header("Root")]
    [SerializeField] private GameObject panel;

    // ===== Card =====
    [Header("Card")]
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text dmgText;

    // ===== CTA =====
    [Header("CTA")]
    [SerializeField] private Button claimButton;
    [SerializeField] private TMP_Text claimCoinText;
    [SerializeField] private Button noThanksButton;

    // ===== Reward default =====
    [Header("Reward (default)")]
    [SerializeField] private int coinReward = 100;

    // ===== Catalog theo loại + level =====
    [System.Serializable]
    public class TypeAssets
    {
        public string unitType;              // khớp Unit.unitType ("Gun", "Knife", ...)
        public Sprite[] portraitByLevel;     // index = level-1
        public string[] nameByLevel;         // optional
        public int[] coinRewardByLevel;      // optional
    }

    [Header("Per-Type Catalog")]
    [SerializeField] private TypeAssets[] catalog;

    // ===== DEV aids =====
    [Header("Dev")]
    [SerializeField] private bool resetUnlocksOnPlayInEditor = false;

    [SerializeField] private bool alwaysShowInEditor = false;

    private const string UnlockKeyPrefix = "unlock_seen_"; // unlock_seen_{type}_L{level}

    // ===== Runtime =====
    private Dictionary<string, TypeAssets> map;
    private readonly Queue<Unit> queue = new Queue<Unit>();
    private Unit current;
    private bool isShowing;

    // ───────────────────────── lifecycle ─────────────────────────
    private void Awake()
    {
        Instance = this;

#if UNITY_EDITOR
        if (resetUnlocksOnPlayInEditor) ResetAllUnlockFlags();
#endif

        if (panel) panel.SetActive(false);
        if (claimButton) claimButton.onClick.AddListener(OnClaim);
        if (noThanksButton) noThanksButton.onClick.AddListener(Close);

        BuildMap();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9)) ResetAllUnlockFlags(); 
    }
#endif

    private void BuildMap()
    {
        map = new Dictionary<string, TypeAssets>();
        if (catalog == null) return;

        for (int i = 0; i < catalog.Length; i++)
        {
            var ta = catalog[i];
            if (ta == null || string.IsNullOrWhiteSpace(ta.unitType)) continue;
            string key = NormType(ta.unitType);
            map[key] = ta;
        }
    }

    // ───────────────────────── public API ─────────────────────────
    public void Enqueue(Unit u)
    {
        if (u == null) return;
        if (!TryMarkFirstTime(u)) return;

        if (isShowing) { queue.Enqueue(u); return; }
        Show(u);
    }

    public void TryShow(Unit u)
    {
        if (u == null) return;
        if (!TryMarkFirstTime(u)) return;
        Show(u);
    }

    // Hiển thị thử cho dev (không cần object Unit)
    public void ShowFor(string unitType, int level, int hp, int dmg, Sprite portraitOverride = null, int rewardOverride = -1)
    {
        current = null;
        isShowing = true;
        FillCardManual(unitType, level, hp, dmg, portraitOverride, rewardOverride);
        OpenPanelOnTop();
    }

    // Xoá 1 flag cụ thể
    public void ResetUnlockFlag(string unitType, int level)
    {
        string key = MakeSeenKey(unitType, level);
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    // Xoá tất cả flag (dựa trên catalog)
    [ContextMenu("DEV: Reset All Unlock Flags")]
    public void ResetAllUnlockFlags()
    {
        if (catalog != null)
        {
            foreach (var ta in catalog)
            {
                if (ta == null || string.IsNullOrWhiteSpace(ta.unitType)) continue;
                string typeKey = NormType(ta.unitType);
                int maxL = 10;
                if (ta.portraitByLevel != null) maxL = Mathf.Max(maxL, ta.portraitByLevel.Length);
                if (ta.nameByLevel != null) maxL = Mathf.Max(maxL, ta.nameByLevel.Length);
                if (ta.coinRewardByLevel != null) maxL = Mathf.Max(maxL, ta.coinRewardByLevel.Length);
                for (int lv = 1; lv <= maxL; lv++)
                    PlayerPrefs.DeleteKey(MakeSeenKey(typeKey, lv));
            }
            PlayerPrefs.Save();
        }
    }

    // ───────────────────────── internals ─────────────────────────
    private void Show(Unit u)
    {
        current = u;
        isShowing = true;
        FillCard(u);
        OpenPanelOnTop();
    }

    private void OpenPanelOnTop()
    {
        if (panel == null) return;

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();

        CanvasGroup cg = panel.GetComponentInParent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        Canvas c = panel.GetComponentInParent<Canvas>();
        if (c != null && c.sortingOrder < 5000)
        {
            c.overrideSorting = true;
            c.sortingOrder = 5000;
        }
    }

    private void FillCard(Unit u)
    {
        if (nameText) nameText.text = u.core != null ? u.core.typeName : u.unitType;
        if (hpText) hpText.text = (u.core != null ? u.core.hpMax : 0).ToString();
        if (dmgText) dmgText.text = (u.core != null ? u.core.dmg : 0).ToString();

        string typeKey = NormType(u.unitType);
        int idx = Mathf.Max(0, u.level - 1);

        if (map != null && map.TryGetValue(typeKey, out var ta))
        {
            if (portrait && ta.portraitByLevel != null && idx < ta.portraitByLevel.Length && ta.portraitByLevel[idx] != null)
                portrait.sprite = ta.portraitByLevel[idx];

            if (nameText && ta.nameByLevel != null && idx < ta.nameByLevel.Length && !string.IsNullOrEmpty(ta.nameByLevel[idx]))
                nameText.text = ta.nameByLevel[idx];

            int reward = coinReward;
            if (ta.coinRewardByLevel != null && idx < ta.coinRewardByLevel.Length && ta.coinRewardByLevel[idx] > 0)
                reward = ta.coinRewardByLevel[idx];
            if (claimCoinText) claimCoinText.text = reward.ToString();
        }
        else
        {
            if (claimCoinText) claimCoinText.text = coinReward.ToString();
        }
    }

    private void FillCardManual(string unitType, int level, int hp, int dmg, Sprite portraitOverride, int rewardOverride)
    {
        if (nameText) nameText.text = unitType;
        if (hpText) hpText.text = hp.ToString();
        if (dmgText) dmgText.text = dmg.ToString();

        string typeKey = NormType(unitType);
        int idx = Mathf.Max(0, level - 1);

        if (map != null && map.TryGetValue(typeKey, out var ta))
        {
            Sprite s = portraitOverride;
            if (s == null && ta.portraitByLevel != null && idx < ta.portraitByLevel.Length)
                s = ta.portraitByLevel[idx];
            if (portrait) portrait.sprite = s;

            if (nameText && ta.nameByLevel != null && idx < ta.nameByLevel.Length && !string.IsNullOrEmpty(ta.nameByLevel[idx]))
                nameText.text = ta.nameByLevel[idx];

            int reward = rewardOverride > 0 ? rewardOverride : coinReward;
            if (ta.coinRewardByLevel != null && idx < ta.coinRewardByLevel.Length && ta.coinRewardByLevel[idx] > 0)
                reward = ta.coinRewardByLevel[idx];
            if (claimCoinText) claimCoinText.text = reward.ToString();
        }
        else
        {
            if (portrait && portraitOverride) portrait.sprite = portraitOverride;
            if (claimCoinText) claimCoinText.text = (rewardOverride > 0 ? rewardOverride : coinReward).ToString();
        }
    }

    private void OnClaim()
    {
        int reward = coinReward;
        if (claimCoinText && int.TryParse(claimCoinText.text, out var parsed)) reward = parsed;
        GameEconomyManager.Instance?.AddCoin(reward);
        Close();
    }

    private void Close()
    {
        current = null;
        isShowing = false;
        if (panel) panel.SetActive(false);

        if (queue.Count > 0) Show(queue.Dequeue());
    }

    // ───────── First-time logic (stable key) ─────────
    private bool TryMarkFirstTime(Unit u)
    {
#if UNITY_EDITOR
        if (alwaysShowInEditor) return true;
#endif
        string key = MakeSeenKey(u.unitType, u.level);
        int seen = PlayerPrefs.GetInt(key, 0);
        if (seen == 1) return false;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        return true;
    }

    private static string MakeSeenKey(string unitType, int level)
    {
        string typeKey = NormType(unitType);
        return $"{UnlockKeyPrefix}{typeKey}_L{level}";
    }

    private static string NormType(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();
    }
}
