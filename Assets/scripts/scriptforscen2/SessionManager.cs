using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Session flow: a list of tasks (pinch reps or hold, mudra holds), shows progress, enqueues tasks,
/// collects per-rep data and at the end writes CSV via CSVExporter.
/// </summary>
public class SessionManager : MonoBehaviour
{
    [Header("References")]
    public OVRHAndsPinch pinchManager;
    public MudraDetectionManager mudraManager;
    public PinchUiManager uiManager;
    public CSVExporter csvExporter;

    [Header("Session Settings")]
    public float targetHoldSeconds = 2f;
    public int repsPerExercise = 8;

    private List<SessionRow> rows = new List<SessionRow>();

    // basic in-memory state for current exercise
    private int currentRep = 0;
    private bool isHolding = false;
    private float holdStartTime = 0f;
    private int currentFinger = 0; // finger index used in current task
    private string currentTaskName = "";

    // Example flow (can be expanded or loaded from config)
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        // Example: run index pinch reps, then middle, then mudra holds
        yield return StartCoroutine(RunPinchReps(0, repsPerExercise)); // index
        yield return StartCoroutine(RunPinchReps(1, repsPerExercise)); // middle
        yield return StartCoroutine(RunMudraHold(MudraType.Surya, 3)); // 3 holds
        // End session -> export CSV
        csvExporter.ExportRows(rows);
        uiManager.UpdateStatusText("Session Complete - CSV saved");
    }

    private IEnumerator RunPinchReps(int fingerIndex, int reps)
    {
        currentFinger = fingerIndex;
        currentTaskName = $"Pinch-Finger{fingerIndex}";
        currentRep = 0;
        uiManager.UpdateStatusText($"Start Pinch: Finger {fingerIndex}");
        while (currentRep < reps)
        {
            // wait for pinch start (use manager events or poll)
            bool gotStart = false;
            float startStrength = 0f;
            // we'll poll smoothed strength
            while (!gotStart)
            {
                float sLeft = pinchManager.GetPinchStrength(pinchManager.leftHand, fingerIndex);
                float sRight = pinchManager.GetPinchStrength(pinchManager.rightHand, fingerIndex);
                if (sLeft >= pinchManager.startThreshold || sRight >= pinchManager.startThreshold)
                {
                    gotStart = true;
                    startStrength = Mathf.Max(sLeft, sRight);
                }
                yield return null;
            }

            // hold until release
            float startTime = Time.time;
            while (true)
            {
                float sLeft = pinchManager.GetPinchStrength(pinchManager.leftHand, fingerIndex);
                float sRight = pinchManager.GetPinchStrength(pinchManager.rightHand, fingerIndex);
                float cur = Mathf.Max(sLeft, sRight);
                // update UI
                uiManager.UpdateFingerStrength(null, fingerIndex, cur);
                if (cur < pinchManager.endThreshold) break;
                yield return null;
            }
            float endTime = Time.time;
            // log rep
            rows.Add(new SessionRow
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                task = currentTaskName,
                rep = currentRep + 1,
                fingerIndex = fingerIndex,
                eventType = "PinchRep",
                duration = (endTime - startTime),
                strength = pinchManager.GetPinchStrength(pinchManager.leftHand, fingerIndex) // approximate final
            });
            currentRep++;
            uiManager.UpdateStatusText($"Completed rep {currentRep}/{reps} for finger {fingerIndex}");
            yield return new WaitForSeconds(0.8f);
        }
    }

    private IEnumerator RunMudraHold(MudraType mudra, int holds)
    {
        currentTaskName = $"Mudra-{mudra}";
        for (int i = 0; i < holds; i++)
        {
            uiManager.UpdateStatusText($"Perform {mudra} hold {i+1}/{holds}");
            // wait until mudra detected by manager
            bool got = false;
            float startT = 0f;
            while (!got)
            {
                if ((mudraManager.leftDetected == mudra) || (mudraManager.rightDetected == mudra))
                {
                    got = true;
                    startT = Time.time;
                }
                yield return null;
            }
            // hold for targetHoldSeconds
            while (Time.time - startT < targetHoldSeconds)
            {
                yield return null;
            }
            rows.Add(new SessionRow
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                task = currentTaskName,
                rep = i + 1,
                fingerIndex = -1,
                eventType = "MudraHold",
                duration = targetHoldSeconds,
                strength = 1f
            });
            uiManager.UpdateStatusText($"Completed mudra hold {i+1}/{holds}");
            yield return new WaitForSeconds(0.8f);
        }
    }

    [Serializable]
    public class SessionRow
    {
        public string timestamp;
        public string task;
        public int rep;
        public int fingerIndex;
        public string eventType; // PinchRep / MudraHold
        public float duration;
        public float strength;
    }
}
