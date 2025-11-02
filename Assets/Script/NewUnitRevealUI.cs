using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class NewUnitRevealUI : MonoBehaviour
{
    public static NewUnitRevealUI Instance { get; private set; }

    #region Serialized Fields

    [Header("Root")]
    [SerializeField] private GameObject panel;

    [Header("Card")]
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text dmgText;

    [Header("CTA")]
    [SerializeField] private Button claimButton;
    [SerializeField] private TMP_Text claimCoinText;
    [SerializeField] private Button noThanksButton;

    [Header("Reward Default")]
    [SerializeField] private int coinReward = 100;

    [System.Serializable]
    public class TypeAssets
    {
        public string unitType;
        public Sprite[] portraitByLevel;
        public string[] nameByLevel;
        public int[] coinRewardByLevel;
    }

    [Header("Per-Type Catalog")]
    [SerializeField] private TypeAssets[] catalog;

    [Header("Dev")]
    [SerializeField] private bool resetUnlocksOnPlayInEditor = false;
    [SerializeField] private bool alwaysShowInEditor = false;

    #endregion

    #region Constants

    private const string UnlockKeyPrefix = "unlock_seen_";

    #endregion

    #region Runtime Fields

    private Dictionary<string, TypeAssets> map;
    private readonly Queue<Unit> queue = new Queue<Unit>();
    private Unit current;
    private bool isShowing;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
#if UNITY_EDITOR
        if (resetUnlocksOnPlayInEditor)
        {
            ResetAllUnlockFlags();
        }
#endif

        if (panel != null) panel.SetActive(false);
        if (noThanksButton != null) noThanksButton.onClick.AddListener(Close);

        BuildMap();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ResetAllUnlockFlags();
        }
    }
#endif

    #endregion

    #region Setup

    private void BuildMap()
    {
        map = new Dictionary<string, TypeAssets>();
        if (catalog == null) return;

        for (int i = 0; i < catalog.Length; i++)
        {
            TypeAssets ta = catalog[i];
            if (ta == null || string.IsNullOrWhiteSpace(ta.unitType)) continue;

            string key = NormType(ta.unitType);
            map[key] = ta;
        }
    }

    #endregion

    #region Public API

    public void Enqueue(Unit u)
    {
        if (u == null) return;
        if (!TryMarkFirstTime(u)) return;

        if (isShowing)
        {
            queue.Enqueue(u);
            return;
        }

        Show(u);
    }

    public void TryShow(Unit u)
    {
        if (u == null) return;
        if (!TryMarkFirstTime(u)) return;
        Show(u);
    }

    public void ShowFor(
        string unitType,
        int level,
        int hp,
        int dmg,
        Sprite portraitOverride = null,
        int rewardOverride = -1
    )
    {
        current = null;
        isShowing = true;

        FillCardManual(unitType, level, hp, dmg, portraitOverride, rewardOverride);
        OpenPanelOnTop();
    }

    public void ResetUnlockFlag(string unitType, int level)
    {
        string key = MakeSeenKey(unitType, level);
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    [ContextMenu("DEV: Reset All Unlock Flags")]
    public void ResetAllUnlockFlags()
    {
        if (catalog == null) return;

        for (int i = 0; i < catalog.Length; i++)
        {
            TypeAssets ta = catalog[i];
            if (ta == null || string.IsNullOrWhiteSpace(ta.unitType)) continue;

            string typeKey = NormType(ta.unitType);
            int maxL = 10;

            if (ta.portraitByLevel != null) maxL = Mathf.Max(maxL, ta.portraitByLevel.Length);
            if (ta.nameByLevel != null) maxL = Mathf.Max(maxL, ta.nameByLevel.Length);
            if (ta.coinRewardByLevel != null) maxL = Mathf.Max(maxL, ta.coinRewardByLevel.Length);

            for (int lv = 1; lv <= maxL; lv++)
            {
                PlayerPrefs.DeleteKey(MakeSeenKey(typeKey, lv));
            }
        }

        PlayerPrefs.Save();
    }

    #endregion

    #region Internal Logic

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

    private void Close()
    {
        current = null;
        isShowing = false;

        if (panel != null) panel.SetActive(false);

        if (queue.Count > 0)
        {
            Show(queue.Dequeue());
        }
    }

    #endregion

    #region Fill Card

    private void FillCard(Unit u)
    {
        if (nameText != null) nameText.text = u.core != null ? u.core.typeName : u.unitType;
        if (hpText != null) hpText.text = (u.core != null ? u.core.hpMax : 0).ToString();
        if (dmgText != null) dmgText.text = (u.core != null ? u.core.dmg : 0).ToString();

        string typeKey = NormType(u.unitType);
        int idx = Mathf.Max(0, u.level - 1);

        if (map != null && map.TryGetValue(typeKey, out TypeAssets ta))
        {
            if (portrait != null && ta.portraitByLevel != null && idx < ta.portraitByLevel.Length && ta.portraitByLevel[idx] != null)
            {
                portrait.sprite = ta.portraitByLevel[idx];
            }

            if (nameText != null && ta.nameByLevel != null && idx < ta.nameByLevel.Length && !string.IsNullOrEmpty(ta.nameByLevel[idx]))
            {
                nameText.text = ta.nameByLevel[idx];
            }

            int reward = coinReward;
            if (ta.coinRewardByLevel != null && idx < ta.coinRewardByLevel.Length && ta.coinRewardByLevel[idx] > 0)
            {
                reward = ta.coinRewardByLevel[idx];
            }

            if (claimCoinText != null) claimCoinText.text = reward.ToString();
        }
        else
        {
            if (claimCoinText != null) claimCoinText.text = coinReward.ToString();
        }
    }

    private void FillCardManual(
        string unitType,
        int level,
        int hp,
        int dmg,
        Sprite portraitOverride,
        int rewardOverride
    )
    {
        if (nameText != null) nameText.text = unitType;
        if (hpText != null) hpText.text = hp.ToString();
        if (dmgText != null) dmgText.text = dmg.ToString();

        string typeKey = NormType(unitType);
        int idx = Mathf.Max(0, level - 1);

        if (map != null && map.TryGetValue(typeKey, out TypeAssets ta))
        {
            Sprite s = portraitOverride;
            if (s == null && ta.portraitByLevel != null && idx < ta.portraitByLevel.Length)
            {
                s = ta.portraitByLevel[idx];
            }

            if (portrait != null) portrait.sprite = s;

            if (nameText != null && ta.nameByLevel != null && idx < ta.nameByLevel.Length && !string.IsNullOrEmpty(ta.nameByLevel[idx]))
            {
                nameText.text = ta.nameByLevel[idx];
            }

            int reward = rewardOverride > 0 ? rewardOverride : coinReward;
            if (ta.coinRewardByLevel != null && idx < ta.coinRewardByLevel.Length && ta.coinRewardByLevel[idx] > 0)
            {
                reward = ta.coinRewardByLevel[idx];
            }

            if (claimCoinText != null) claimCoinText.text = reward.ToString();
        }
        else
        {
            if (portrait != null && portraitOverride != null)
            {
                portrait.sprite = portraitOverride;
            }

            if (claimCoinText != null)
            {
                int reward = rewardOverride > 0 ? rewardOverride : coinReward;
                claimCoinText.text = reward.ToString();
            }
        }
    }

    #endregion

    #region Unlock Flags

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
        return UnlockKeyPrefix + typeKey + "_L" + level;
    }

    private static string NormType(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();
    }

    #endregion
}
