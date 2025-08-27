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
        SetupLevel(currentLevel); // chạy level đầu tiên
    }

    public void NextLevel()
    {
        if (currentLevel < maxLevel)
        {
            currentLevel++;
            SetupLevel(currentLevel);
        }
        else
        {
            Debug.Log("🎉 Đã hoàn thành tất cả các level!");
        }
    }

    public void SetupLevel(int level)
    {
        Debug.Log($"🔄 SetupLevel {level}");

        // 1. Clear units cũ
        gridManager.ClearAllUnits();

        // 2. Reset game state
        gameManager.ResetBattle();

        // 3. Spawn enemy theo level
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

        // 4. Đổi background (nếu có)
        ChangeBackground(level);
    }

    private void ChangeBackground(int level)
    {
        // Ví dụ: đổi màu background theo level
        Camera.main.backgroundColor = level switch
        {
            1 => Color.green,
            2 => Color.blue,
            3 => Color.red,
            _ => Color.black,
        };
    }
}
