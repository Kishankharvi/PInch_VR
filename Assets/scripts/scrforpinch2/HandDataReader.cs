using UnityEngine;
using System.Collections.Generic;

public class HandDataReader : MonoBehaviour
{
    [Header("Hand References")]
    public OVRHand leftHand;
    public OVRHand rightHand;

    public Dictionary<OVRHand.HandFinger, float> LeftHandPinchStrengths { get; private set; }
    public Dictionary<OVRHand.HandFinger, float> RightHandPinchStrengths { get; private set; }
    public Vector3 LeftHandPosition { get; private set; }
    public Vector3 RightHandPosition { get; private set; }
    
    private readonly OVRHand.HandFinger[] allFingers = {
        OVRHand.HandFinger.Thumb, OVRHand.HandFinger.Index, 
        OVRHand.HandFinger.Middle, OVRHand.HandFinger.Ring, OVRHand.HandFinger.Pinky
    };

    void Awake()
    {
        LeftHandPinchStrengths = new Dictionary<OVRHand.HandFinger, float>();
        RightHandPinchStrengths = new Dictionary<OVRHand.HandFinger, float>();
        foreach (var finger in allFingers)
        {
            LeftHandPinchStrengths[finger] = 0f;
            RightHandPinchStrengths[finger] = 0f;
        }
    }

    void Update()
    {
        UpdateHandData(leftHand, LeftHandPinchStrengths);
        UpdateHandData(rightHand, RightHandPinchStrengths);

        if (leftHand != null && leftHand.IsTracked) LeftHandPosition = leftHand.transform.position;
        if (rightHand != null && rightHand.IsTracked) RightHandPosition = rightHand.transform.position;
    }

    private void UpdateHandData(OVRHand hand, Dictionary<OVRHand.HandFinger, float> pinchData)
    {
        if (hand == null || !hand.IsTracked)
        {
            foreach (var finger in allFingers) pinchData[finger] = 0f;
            return;
        }

        foreach (var finger in allFingers)
        {
            pinchData[finger] = hand.GetFingerPinchStrength(finger);
        }
    }
}