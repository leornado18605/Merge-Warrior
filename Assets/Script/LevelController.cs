using UnityEngine;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BotManager botManager;
    [SerializeField] private GameManager gameManager;

    [Header("Level Settings")]
    public int currentLevel = 1;
    public int maxLevel = 5;

    [SerializeField] private float resultDelay = 0.6f;
    private bool endBattleScheduled = false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetupLevel(currentLevel); 
    }

    public void NextLevel()
    {
        if (currentLevel < maxLevel)
        {
            currentLevel++;
            SetupLevel(currentLevel);
            Debug.Log($"➡️ Moving to level {currentLevel}");
        }
    }

    public void SetupLevel(int level)
    {
        Debug.Log($"🔄 SetupLevel {level}");

        gridManager.ClearAllUnits();

        gameManager.ResetBattle();

        switch (level)
        {
            case 1:
                botManager.SpawnBot(GridManager.Board.Board2, 2, 3, botManager.botLevelPrefabs, 5);
                break;
            case 2:
                botManager.SpawnBot(GridManager.Board.Board2, 3, 4, botManager.botLevelPrefabs, 8);
                break;
            case 3:
                botManager.SpawnBot(GridManager.Board.Board2, 4, 5, botManager.botLevelPrefabs, 10);
                break;
            default:
                botManager.SpawnBot(GridManager.Board.Board2, 5, 5, botManager.botLevelPrefabs, 12);
                break;
        }

        ChangeBackground(level);
    }

    private void ChangeBackground(int level)
    {
        Camera.main.backgroundColor = level switch
        {
            1 => Color.green,
            2 => Color.blue,
            3 => Color.red,
            _ => Color.black,
        };
    }
}
