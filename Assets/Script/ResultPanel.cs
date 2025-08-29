//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;   // nếu bạn muốn đổi text nút

//public class ResultPanel : MonoBehaviour
//{
//    [Header("One Button")]
//    [SerializeField] private Button actionButton;
//    [SerializeField] private TMP_Text actionLabel;   // optional: đổi chữ trên nút
//    [SerializeField] private Button homeBtn;         // optional: về Level 1

//    // lưu action hiện tại của nút
//    private System.Action _currentAction;

//    void Awake()
//    {
//        if (actionButton) actionButton.onClick.AddListener(() => _currentAction?.Invoke());
//        if (homeBtn) homeBtn.onClick.AddListener(() => LevelController.Instance.LoadFirst());
//        gameObject.SetActive(false);
//    }

//    public void Hide() => gameObject.SetActive(false);
    
//    // ======= API gọi từ UI/GameManager =======
//    public void ShowWin()
//    {
//        gameObject.SetActive(true);

//        _currentAction = () => LevelController.Instance.LoadNext();

//        if (actionLabel) actionLabel.text = "Next";  // đổi label nếu muốn
//        if (actionButton)
//        {
//            actionButton.interactable = LevelController.Instance.HasNext();
//            actionButton.gameObject.SetActive(true);
//        }
//    }

//    public void ShowLose()
//    {
//        gameObject.SetActive(true);

//        _currentAction = () => LevelController.Instance.Reload();

//        if (actionLabel) actionLabel.text = "Retry";
//        if (actionButton)
//        {
//            actionButton.interactable = true;
//            actionButton.gameObject.SetActive(true);
//        }
//    }
//}
