using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction.Input;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic; // Correct namespace for OVRHand
using UnityEngine.UI; // Add this for Slider


/// <summary>
/// Tracks finger pinch strength, updates UI sliders, and provides events for rehabilitation exercises.
/// </summary>
public class OVRHAndsPinch : MonoBehaviour
{
    [Header("Hand References")]
    public OVRHand leftHand;
    public OVRHand rightHand;
    public OVRSkeleton leftSkeleton;
    public OVRSkeleton rightSkeleton;

    [Header("UI Sliders (for 5 fingers)")]
    public UnityEngine.UI.Slider[] fingerSliders; // Assign 5 sliders in inspector for Thumb, Index, Middle, Ring, Pinky

    [Header("Pinch Settings")]
    public bool useOVRHandPinchStrength = true; // Toggle between OVR pinch and distance-based calculation
    [Range(0.01f, 0.1f)] public float pinchDistanceMin = 0.015f;
    [Range(0.02f, 0.1f)] public float pinchDistanceMax = 0.05f;
    [Range(0f, 1f)] public float startThreshold = 0.6f;
    [Range(0f, 1f)] public float endThreshold = 0.45f;
    [Range(0.01f, 1f)] public float smoothing = 0.15f;
    [Range(0.001f, 0.05f)] public float microMovementDelta = 0.01f;

    [Header("Debug Options")]
    public bool showDebugLogs = false;

    // Events for rehab system
    [Serializable] public class PinchEvent : UnityEvent<OVRHand, int, float> { } // hand, fingerIndex, strength
    [Serializable] public class PinchMicroEvent : UnityEvent<OVRHand, int> { } // hand, fingerIndex

    public PinchEvent OnPinchStart;
    public PinchEvent OnPinchHold;
    public PinchEvent OnPinchEnd;
    public PinchMicroEvent OnPinchMicroMovement;

    private Dictionary<(OVRHand, int), float> smoothedStrength = new();
    private Dictionary<(OVRHand, int), float> lastReportedStrength = new();
    private Dictionary<(OVRHand, int), bool> isPinched = new();

    private readonly OVRHand.HandFinger[] fingerEnums =
    {
        OVRHand.HandFinger.Thumb,
        OVRHand.HandFinger.Index,
        OVRHand.HandFinger.Middle,
        OVRHand.HandFinger.Ring,
        OVRHand.HandFinger.Pinky
    };

    void Update()
    {
        // Process each hand every frame
        ProcessHand(leftHand, leftSkeleton);
        ProcessHand(rightHand, rightSkeleton);
    }

    /// <summary>
    /// Processes pinch data for each hand and updates UI + state.
    /// </summary>
    private void ProcessHand(OVRHand hand, OVRSkeleton skeleton)
    {
        // If hand is not tracked, reset all values to 0
        if (hand == null || !hand.IsTracked)
        {
            for (int i = 0; i < fingerEnums.Length; i++)
            {
                var key = (hand, i);
                smoothedStrength[key] = 0f;
                UpdateFingerStrength(i, 0f);
            }
            return;
        }

        // Loop through each finger
        for (int i = 0; i < fingerEnums.Length; i++)
        {
            float rawStrength = useOVRHandPinchStrength
                ? hand.GetFingerPinchStrength(fingerEnums[i])
                : CalculateDistanceStrength(skeleton, i);

            var key = (hand, i);

            // Initialize if first time
            if (!smoothedStrength.ContainsKey(key))
                smoothedStrength[key] = rawStrength;

            // Smooth pinch strength with frame rate independent lerp
            smoothedStrength[key] = Mathf.Lerp(smoothedStrength[key], rawStrength, Time.deltaTime * (1f / smoothing));
            smoothedStrength[key] = Mathf.Clamp01(smoothedStrength[key]);

            // Update UI slider
            UpdateFingerStrength(i, smoothedStrength[key]);

            // Track micro-movement changes
            if (!lastReportedStrength.ContainsKey(key))
                lastReportedStrength[key] = smoothedStrength[key];

            if (Mathf.Abs(smoothedStrength[key] - lastReportedStrength[key]) >= microMovementDelta)
            {
                OnPinchMicroMovement?.Invoke(hand, i);
                lastReportedStrength[key] = smoothedStrength[key];
            }

            // Detect pinch state transitions
            if (!isPinched.ContainsKey(key)) isPinched[key] = false;

            if (!isPinched[key])
            {
                // Pinch start
                if (smoothedStrength[key] >= startThreshold)
                {
                    isPinched[key] = true;
                    OnPinchStart?.Invoke(hand, i, smoothedStrength[key]);
                    if (showDebugLogs) Debug.Log($"Pinch Start - {fingerEnums[i]} Strength: {smoothedStrength[key]}");
                }
            }
            else
            {
                // Pinch hold
                OnPinchHold?.Invoke(hand, i, smoothedStrength[key]);

                // Pinch end
                if (smoothedStrength[key] < endThreshold)
                {
                    isPinched[key] = false;
                    OnPinchEnd?.Invoke(hand, i, smoothedStrength[key]);
                    if (showDebugLogs) Debug.Log($"Pinch End - {fingerEnums[i]}");
                }
            }
        }
    }

    /// <summary>
    /// Distance-based calculation for pinch strength if OVRHand pinch strength is unavailable.
    /// </summary>
    private float CalculateDistanceStrength(OVRSkeleton skeleton, int fingerIndex)
    {
        if (skeleton == null || skeleton.Bones == null || skeleton.Bones.Count == 0)
            return 0f;

        Transform thumbTip = null;
        Transform fingerTip = null;

        // Thumb tip bone index
        int thumbTipIndex = 19;

        // Finger tip bone index map
        int[] tipIndices = { 19, 20, 21, 22, 23 }; // Thumb, Index, Middle, Ring, Pinky
        int fingerTipIndex = tipIndices[fingerIndex];

        foreach (var bone in skeleton.Bones)
        {
            if (bone.Id == (OVRSkeleton.BoneId)thumbTipIndex) thumbTip = bone.Transform;
            if (bone.Id == (OVRSkeleton.BoneId)fingerTipIndex) fingerTip = bone.Transform;
        }

        if (thumbTip == null || fingerTip == null)
            return 0f;

        float distance = Vector3.Distance(thumbTip.position, fingerTip.position);

        // Convert distance to normalized strength
        float normalized = Mathf.InverseLerp(pinchDistanceMax, pinchDistanceMin, distance);
        return Mathf.Clamp01(normalized);
    }

    /// <summary>
    /// Updates a specific UI slider with the current finger strength.
    /// </summary>
    public void UpdateFingerStrength(int fingerIndex, float strength)
    {
        if (fingerIndex >= 0 && fingerIndex < fingerSliders.Length && fingerSliders[fingerIndex] != null)
        {
            fingerSliders[fingerIndex].value = Mathf.Clamp01(strength);
        }
    }

    internal float GetPinchStrength(OVRHand leftHand, int fingerIndex)
    {
        throw new NotImplementedException();
    }
}




































// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Events;

// [Serializable]
// public class FingerPinchEvents : UnityEvent<OVRHand, int, float> { } // hand, fingerIndex, strength
// [Serializable]
// public class SimpleFingerEvents : UnityEvent<OVRHand, int> { }

// /// <summary>
// /// Detects pinch strengths using OVRHand.GetFingerPinchStrength and optional skeleton distances.
// /// Fires start/hold/end events and micro movement events.
// /// </summary>
// public class OVRHAndsPinch : MonoBehaviour
// {
//     [Header("Hand References")]
//     public OVRHand leftHand;
//     public OVRHand rightHand;
//     public OVRSkeleton leftSkeleton;   // optional: for distance fallback or advanced detection
//     public OVRSkeleton rightSkeleton;

//     [Header("Settings")]
//     [Tooltip("If true uses OVRHand.GetFingerPinchStrength, else uses skeleton thumb->tip distances.")]
//     public bool useOVRHandPinchStrength = true;
//     public float pinchDistanceMin = 0.015f;
//     public float pinchDistanceMax = 0.05f;

//     [Range(0f,1f)] public float startThreshold = 0.6f;
//     [Range(0f,1f)] public float endThreshold = 0.45f;
//     [Range(0f,1f)] public float smoothing = 0.15f;
//     public float microMovementDelta = 0.01f;

//     [Header("Events (OVRHand, fingerIndex, strength)")]
//     public FingerPinchEvent OnPinchStart;
//     public FingerPinchEvent OnPinchHold;
//     public FingerPinchEvent OnPinchEnd;
//     [Header("Micro movement (OVRHand, fingerIndex)")]
//     public SimpleFingerEvent OnPinchMicroMovement;

//     // internal state keyed by (OVRHand, fingerIndex)
//     private Dictionary<(OVRHand, int), float> smoothedStrength = new();
//     private Dictionary<(OVRHand, int), bool> isPinched = new();
//     private Dictionary<(OVRHand, int), float> lastReportedStrength = new();

//     private readonly OVRHand.HandFinger[] fingerEnums = {
//         OVRHand.HandFinger.Index,
//         OVRHand.HandFinger.Middle,
//         OVRHand.HandFinger.Ring,
//         OVRHand.HandFinger.Pinky
//     };

//     void Update()
//     {
//         ProcessHand(leftHand, leftSkeleton);
//         ProcessHand(rightHand, rightSkeleton);
//     }

//     private void ProcessHand(OVRHand hand, OVRSkeleton skeleton)
//     {
//         if (hand == null || !hand.IsTracked) return;

//         for (int i = 0; i < fingerEnums.Length; i++)
//         {
//             float rawStrength = 0f;
//             if (useOVRHandPinchStrength)
//             {
//                 rawStrength = hand.GetFingerPinchStrength(fingerEnums[i]); // 0..1
//             }
//             else
//             {
//                 // fallback: use thumb->tip distance from skeleton if available
//                 if (skeleton != null && skeleton.Bones != null && skeleton.Bones.Count > 0)
//                 {
//                     Vector3 thumb = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_ThumbTip].Transform.position;
//                     OVRSkeleton.BoneId tipId = OVRSkeleton.BoneId.Hand_IndexTip;
//                     switch (i)
//                     {
//                         case 0: tipId = OVRSkeleton.BoneId.Hand_IndexTip; break;
//                         case 1: tipId = OVRSkeleton.BoneId.Hand_MiddleTip; break;
//                         case 2: tipId = OVRSkeleton.BoneId.Hand_RingTip; break;
//                         case 3: tipId = OVRSkeleton.BoneId.Hand_PinkyTip; break;
//                     }
//                     Vector3 tip = skeleton.Bones[(int)tipId].Transform.position;
//                     float dist = Vector3.Distance(thumb, tip);
//                     rawStrength = DistanceToStrength(dist);
//                 }
//                 else
//                 {
//                     rawStrength = 0f;
//                 }
//             }

//             var key = (hand, i);
//             if (!smoothedStrength.ContainsKey(key)) smoothedStrength[key] = rawStrength;
//             smoothedStrength[key] = Mathf.Lerp(smoothedStrength[key], rawStrength, smoothing);

//             if (!lastReportedStrength.ContainsKey(key)) lastReportedStrength[key] = smoothedStrength[key];
//             if (Mathf.Abs(smoothedStrength[key] - lastReportedStrength[key]) >= microMovementDelta)
//             {
//                 try { OnPinchMicroMovement?.Invoke(hand, i); } catch { }
//                 lastReportedStrength[key] = smoothedStrength[key];
//             }

//             if (!isPinched.ContainsKey(key)) isPinched[key] = false;

//             if (!isPinched[key])
//             {
//                 if (smoothedStrength[key] >= startThreshold)
//                 {
//                     isPinched[key] = true;
//                     try { OnPinchStart?.Invoke(hand, i, smoothedStrength[key]); } catch { }
//                 }
//             }
//             else
//             {
//                 try { OnPinchHold?.Invoke(hand, i, smoothedStrength[key]); } catch { }

//                 if (smoothedStrength[key] < endThreshold)
//                 {
//                     isPinched[key] = false;
//                     try { OnPinchEnd?.Invoke(hand, i, smoothedStrength[key]); } catch { }
//                 }
//             }
//         }
//     }

//     private float DistanceToStrength(float dist)
//     {
//         if (dist <= pinchDistanceMin) return 1f;
//         if (dist >= pinchDistanceMax) return 0f;
//         float t = (dist - pinchDistanceMin) / (pinchDistanceMax - pinchDistanceMin);
//         return 1f - t;
//     }

//     // public accessor
//     public float GetPinchStrength(OVRHand hand, int fingerIndex)
//     {
//         var key = (hand, fingerIndex);
//         return smoothedStrength.ContainsKey(key) ? smoothedStrength[key] : 0f;
//     }
// }
