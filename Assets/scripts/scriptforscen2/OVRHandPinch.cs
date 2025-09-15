using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class FingerPinchEventt : UnityEvent<OVRHand, int, float> { } // hand, fingerIndex, strength
[Serializable]
public class SimpleFingerEventt : UnityEvent<OVRHand, int> { }

/// <summary>
/// Detects pinch strengths using OVRHand.GetFingerPinchStrength and optional skeleton distances.
/// Fires start/hold/end events and micro movement events.
/// </summary>
public class OVRHandPinch : MonoBehaviour
{
    [Header("Hand References")]
    public OVRHand leftHand;
    public OVRHand rightHand;
    public OVRSkeleton leftSkeleton;   // optional: for distance fallback or advanced detection
    public OVRSkeleton rightSkeleton;

    [Header("Settings")]
    [Tooltip("If true uses OVRHand.GetFingerPinchStrength, else uses skeleton thumb->tip distances.")]
    public bool useOVRHandPinchStrength = true;
    public float pinchDistanceMin = 0.015f;
    public float pinchDistanceMax = 0.05f;

    [Range(0f,1f)] public float startThreshold = 0.6f;
    [Range(0f,1f)] public float endThreshold = 0.45f;
    [Range(0f,1f)] public float smoothing = 0.15f;
    public float microMovementDelta = 0.01f;

    [Header("Events (OVRHand, fingerIndex, strength)")]
    public FingerPinchEvent OnPinchStart;
    public FingerPinchEvent OnPinchHold;
    public FingerPinchEvent OnPinchEnd;
    [Header("Micro movement (OVRHand, fingerIndex)")]
    public SimpleFingerEvent OnPinchMicroMovement;

    // internal state keyed by (OVRHand, fingerIndex)
    private Dictionary<(OVRHand, int), float> smoothedStrength = new();
    private Dictionary<(OVRHand, int), bool> isPinched = new();
    private Dictionary<(OVRHand, int), float> lastReportedStrength = new();

    private readonly OVRHand.HandFinger[] fingerEnums = {
        OVRHand.HandFinger.Index,
        OVRHand.HandFinger.Middle,
        OVRHand.HandFinger.Ring,
        OVRHand.HandFinger.Pinky
    };

    void Update()
    {
        ProcessHand(leftHand, leftSkeleton);
        ProcessHand(rightHand, rightSkeleton);
    }

    private void ProcessHand(OVRHand hand, OVRSkeleton skeleton)
    {
        if (hand == null || !hand.IsTracked) return;

        for (int i = 0; i < fingerEnums.Length; i++)
        {
            float rawStrength = 0f;
            if (useOVRHandPinchStrength)
            {
                rawStrength = hand.GetFingerPinchStrength(fingerEnums[i]); // 0..1
            }
            else
            {
                // fallback: use thumb->tip distance from skeleton if available
                if (skeleton != null && skeleton.Bones != null && skeleton.Bones.Count > 0)
                {
                    Vector3 thumb = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_ThumbTip].Transform.position;
                    OVRSkeleton.BoneId tipId = OVRSkeleton.BoneId.Hand_IndexTip;
                    switch (i)
                    {
                        case 0: tipId = OVRSkeleton.BoneId.Hand_IndexTip; break;
                        case 1: tipId = OVRSkeleton.BoneId.Hand_MiddleTip; break;
                        case 2: tipId = OVRSkeleton.BoneId.Hand_RingTip; break;
                        case 3: tipId = OVRSkeleton.BoneId.Hand_PinkyTip; break;
                    }
                    Vector3 tip = skeleton.Bones[(int)tipId].Transform.position;
                    float dist = Vector3.Distance(thumb, tip);
                    rawStrength = DistanceToStrength(dist);
                }
                else
                {
                    rawStrength = 0f;
                }
            }

            var key = (hand, i);
            if (!smoothedStrength.ContainsKey(key)) smoothedStrength[key] = rawStrength;
            smoothedStrength[key] = Mathf.Lerp(smoothedStrength[key], rawStrength, smoothing);

            if (!lastReportedStrength.ContainsKey(key)) lastReportedStrength[key] = smoothedStrength[key];
            if (Mathf.Abs(smoothedStrength[key] - lastReportedStrength[key]) >= microMovementDelta)
            {
                try { OnPinchMicroMovement?.Invoke(hand, i); } catch { }
                lastReportedStrength[key] = smoothedStrength[key];
            }

            if (!isPinched.ContainsKey(key)) isPinched[key] = false;

            if (!isPinched[key])
            {
                if (smoothedStrength[key] >= startThreshold)
                {
                    isPinched[key] = true;
                    try { OnPinchStart?.Invoke(hand, i, smoothedStrength[key]); } catch { }
                }
            }
            else
            {
                try { OnPinchHold?.Invoke(hand, i, smoothedStrength[key]); } catch { }

                if (smoothedStrength[key] < endThreshold)
                {
                    isPinched[key] = false;
                    try { OnPinchEnd?.Invoke(hand, i, smoothedStrength[key]); } catch { }
                }
            }
        }
    }

    private float DistanceToStrength(float dist)
    {
        if (dist <= pinchDistanceMin) return 1f;
        if (dist >= pinchDistanceMax) return 0f;
        float t = (dist - pinchDistanceMin) / (pinchDistanceMax - pinchDistanceMin);
        return 1f - t;
    }

    // public accessor
    public float GetPinchStrength(OVRHand hand, int fingerIndex)
    {
        var key = (hand, fingerIndex);
        return smoothedStrength.ContainsKey(key) ? smoothedStrength[key] : 0f;
    }
}
