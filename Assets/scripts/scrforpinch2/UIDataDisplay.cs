using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDataDisplay : MonoBehaviour
{
    [Header("System References")]
    public HandDataReader handDataReader;
    public SessionsManager sessionManager;

    [Header("UI Panels")]
    public GameObject preSessionPanel;
    public GameObject activeSessionPanel;
    public GameObject postSessionPanel;
    
    [Header("UI Elements")]
    public Button startButton;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI taskProgressText;
    public Slider[] pinchSliders; // Size 5
    public TextMeshProUGUI handPositionText;
    public TextMeshProUGUI reportText;

    void Start()
    {
        startButton.onClick.AddListener(sessionManager.StartSession);
        ShowPreSessionUI();
    }

    void Update()
    {
        if (!activeSessionPanel.activeSelf) return;

        for (int i = 0; i < 5; i++)
        {
            pinchSliders[i].value = handDataReader.RightHandPinchStrengths[(OVRHand.HandFinger)i];
        }
        Vector3 pos = handDataReader.RightHandPosition;
        handPositionText.text = $"Hand Position:\nX: {pos.x:F2}  Y: {pos.y:F2}  Z: {pos.z:F2}";
    }

    public void ShowPreSessionUI()
    {
        preSessionPanel.SetActive(true);
        activeSessionPanel.SetActive(false);
        postSessionPanel.SetActive(false);
    }
    
    public void ShowSessionActiveUI()
    {
        preSessionPanel.SetActive(false);
        activeSessionPanel.SetActive(true);
        postSessionPanel.SetActive(false);
    }
    
    public void ShowSessionCompleteUI(string report)
    {
        preSessionPanel.SetActive(false);
        activeSessionPanel.SetActive(false);
        postSessionPanel.SetActive(true);
        reportText.text = report;
    }
    
    public void UpdateInstructionText(string text) { instructionText.text = text; }
    public void UpdateTaskProgress(string text) { taskProgressText.text = text; }
}