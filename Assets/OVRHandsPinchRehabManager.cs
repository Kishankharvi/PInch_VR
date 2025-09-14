using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction.Input; // Correct namespace for OVRHand

[Serializable]
public class FingerPinchEvent : UnityEvent<OVRHand, int, float> { } // hand, fingerIndex, strength

[Serializable]
public class SimpleFingerEvent : UnityEvent<OVRHand, int> { }

public class OVRHandsPinchRehabManager : MonoBehaviour
{
    [Header("Hand References")]
    public OVRHand leftHand;
    public OVRHand rightHand;

    [Header("Pinch thresholds (meters)")]
    [Tooltip("Minimum distance to consider a pinch (fingers close together)")]
    public float pinchDistanceMin = 0.015f;

    [Tooltip("Maximum distance for an open hand (no pinch)")]
    public float pinchDistanceMax = 0.05f;

    [Header("Thresholds & Smoothing")]
    [Range(0f, 1f)] public float startThreshold = 0.6f;
    [Range(0f, 1f)] public float endThreshold = 0.45f;
    [Range(0f, 1f)] public float smoothing = 0.15f;

    [Tooltip("Amount of small change needed to trigger micro-movement event")]
    public float microMovementDelta = 0.01f;

    [Header("Events")]
    public FingerPinchEvent OnPinchStart;
    public FingerPinchEvent OnPinchHold;
    public FingerPinchEvent OnPinchEnd;
    public SimpleFingerEvent OnPinchMicroMovement;

    private readonly Dictionary<(OVRHand, int), float> smoothedStrength = new();
    private readonly Dictionary<(OVRHand, int), bool> isPinched = new();
    private readonly Dictionary<(OVRHand, int), float> lastReportedStrength = new();

    void Update()
    {
        ProcessHand(leftHand);
        ProcessHand(rightHand);
    }

    private void ProcessHand(OVRHand hand)
    {
        if (hand == null || !hand.IsTracked) return;

        // Iterate through each finger except the thumb
        for (int i = 0; i < 4; i++)
        {
            var finger = (OVRHand.HandFinger)(i + 1); // Index, Middle, Ring, Pinky
            float rawStrength = hand.GetFingerPinchStrength(finger);

            var key = (hand, i);

            // Initialize smoothed strength if it doesn't exist
            if (!smoothedStrength.ContainsKey(key))
                smoothedStrength[key] = rawStrength;

            // Apply smoothing
            smoothedStrength[key] = Mathf.Lerp(smoothedStrength[key], rawStrength, 1f - smoothing);

            // Micro-movement detection
            if (!lastReportedStrength.ContainsKey(key))
                lastReportedStrength[key] = smoothedStrength[key];

            if (Mathf.Abs(smoothedStrength[key] - lastReportedStrength[key]) >= microMovementDelta)
            {
                OnPinchMicroMovement.Invoke(hand, i);
                lastReportedStrength[key] = smoothedStrength[key];
            }

            // Pinch state transitions
            if (!isPinched.ContainsKey(key))
                isPinched[key] = false;

            if (!isPinched[key])
            {
                // Start Pinch
                if (smoothedStrength[key] >= startThreshold)
                {
                    isPinched[key] = true;
                    OnPinchStart.Invoke(hand, i, smoothedStrength[key]);
                }
            }
            else
            {
                // Pinch Hold
                OnPinchHold.Invoke(hand, i, smoothedStrength[key]);

                // End Pinch
                if (smoothedStrength[key] < endThreshold)
                {
                    isPinched[key] = false;
                    OnPinchEnd.Invoke(hand, i, smoothedStrength[key]);
                }
            }
        }
    }
}
