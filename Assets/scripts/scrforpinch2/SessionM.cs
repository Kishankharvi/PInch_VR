using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;

// --- Data Structures (place at the top of the file) ---
public enum TaskType { PinchAndHold, SuccessivePinches }

[System.Serializable]
public class RehabTask
{
    public string instruction;
    public TaskType taskType;
    public OVRHand.HandFinger fingerToTrack;
    public float targetStrength = 0.8f;
    public float holdDuration = 3.0f;
    public int pinchCount = 5;
}

[System.Serializable]
public class RehabSessionData
{
    public string sessionDate;
    public List<float> maxPinchStrength = new List<float>(new float[5]);
}
// --- Main Class ---
public class SessionsManager : MonoBehaviour
{
    [Header("System References")]
    public HandDataReader handDataReader;
    public UIDataDisplay uiDisplay;

    [Header("Task Sequence")]
    public List<RehabTask> taskSequence;

    // Events for other scripts to listen to
    [System.Serializable] public class PinchEvent : UnityEvent<OVRHand, int> { }
    public PinchEvent OnPinchStarted;
    public PinchEvent OnPinchEnded;
    
    private RehabSessionData currentSessionData;
    private RehabSessionData previousSessionData;
    private int currentTaskIndex = -1;
    private RehabTask currentTask;
    private bool isSessionActive = false;
    private float holdTimer = 0f;
    private int successfulPinches = 0;
    private bool isPinching = false; // To track state for events

    public void StartSession()
    {
        LoadData();
        currentSessionData = new RehabSessionData { sessionDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
        isSessionActive = true;
        uiDisplay.ShowSessionActiveUI();
        currentTaskIndex = -1;
        AdvanceToNextTask();
    }
    
    void Update()
    {
        if (!isSessionActive) return;

        var pinchStrength = handDataReader.RightHandPinchStrengths[currentTask.fingerToTrack];

        // --- Event Firing for Visualizer ---
        if (pinchStrength >= currentTask.targetStrength && !isPinching)
        {
            isPinching = true;
            OnPinchStarted?.Invoke(handDataReader.rightHand, (int)currentTask.fingerToTrack);
        }
        else if (pinchStrength < currentTask.targetStrength && isPinching)
        {
            isPinching = false;
            OnPinchEnded?.Invoke(handDataReader.rightHand, (int)currentTask.fingerToTrack);
        }
        
        // --- Task Logic ---
        if (currentTask.taskType == TaskType.PinchAndHold)
        {
            if (pinchStrength >= currentTask.targetStrength) holdTimer += Time.deltaTime;
            else holdTimer = 0;

            if (holdTimer >= currentTask.holdDuration) AdvanceToNextTask();
            uiDisplay.UpdateTaskProgress($"Hold: {holdTimer:F1}s / {currentTask.holdDuration:F1}s");
        }
        // Add SuccessivePinches logic here if needed...

        // --- Data Recording ---
        for (int i = 0; i < 5; i++)
        {
            var finger = (OVRHand.HandFinger)i;
            float strength = handDataReader.RightHandPinchStrengths[finger];
            if (strength > currentSessionData.maxPinchStrength[i])
            {
                currentSessionData.maxPinchStrength[i] = strength;
            }
        }
    }
    
    private void AdvanceToNextTask()
    {
        if (isPinching) OnPinchEnded?.Invoke(handDataReader.rightHand, (int)currentTask.fingerToTrack);
        
        holdTimer = 0f;
        successfulPinches = 0;
        isPinching = false;
        currentTaskIndex++;

        if (currentTaskIndex >= taskSequence.Count)
        {
            EndSession();
            return;
        }
        currentTask = taskSequence[currentTaskIndex];
        uiDisplay.UpdateInstructionText(currentTask.instruction);
    }

    private void EndSession()
    {
        isSessionActive = false;
        SaveData();
        string report = CompareSessions();
        uiDisplay.ShowSessionCompleteUI(report);
    }

    private string CompareSessions()
    {
        if (previousSessionData == null) return "First session complete! Data saved for next time.";
        string report = "Session Comparison (Max Strength):\n";
        for (int i = 0; i < 5; i++)
        {
            float current = currentSessionData.maxPinchStrength[i];
            float previous = previousSessionData.maxPinchStrength[i];
            float improvement = ((current - previous) / (previous == 0 ? 1 : previous)) * 100;
            report += $"{(OVRHand.HandFinger)i}: {current:P0} (";
            if(improvement > 5) report += $"<color=green>+{improvement:F0}%</color>)\n";
            else if (improvement < -5) report += $"<color=red>{improvement:F0}%</color>)\n";
            else report += "Stable)\n";
        }
        return report;
    }

    public void SaveData()
    {
        string json = JsonUtility.ToJson(currentSessionData, true);
        File.WriteAllText(Application.persistentDataPath + "/rehabData.json", json);
    }

    public void LoadData()
    {
        string path = Application.persistentDataPath + "/rehabData.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            previousSessionData = JsonUtility.FromJson<RehabSessionData>(json);
        }
    }
}