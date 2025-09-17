using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class MudraDetector : MonoBehaviour
{
    public OVRHand leftHand;
    public OVRHand rightHand;

    [System.Serializable]
    public class MudraEvent : UnityEvent<string> { }
    public MudraEvent OnMudraDetected;

    // Use a Dictionary to track the mudra state for each hand separately
    private Dictionary<OVRHand, string> currentMudras = new Dictionary<OVRHand, string>();

    void Start()
    {
        // Initialize the state for both hands
        if (leftHand != null) currentMudras[leftHand] = "None";
        if (rightHand != null) currentMudras[rightHand] = "None";
    }

    void Update()
    {
        DetectMudra(leftHand, "Left");
        DetectMudra(rightHand, "Right");
    }

    void DetectMudra(OVRHand hand, string side)
    {
        if (hand == null || !hand.IsTracked) return;

        // Ensure the hand is initialized in the dictionary
        if (!currentMudras.ContainsKey(hand)) currentMudras[hand] = "None";

        string detectedMudra = "None";
        
        // Example Mudra: Surya Mudra - Thumb + Ring
        bool isThumbPinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Thumb);
        bool isRingPinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Ring);

        if (isThumbPinching && isRingPinching)
        {
            detectedMudra = "Surya Mudra";
        }
        
        // Add other mudra detection logic here...
        // else if (isThumbPinching && isIndexPinching) { detectedMudra = "Jnana Mudra"; }

        // Check if the state for THIS SPECIFIC HAND has changed
        if (detectedMudra != currentMudras[hand])
        {
            currentMudras[hand] = detectedMudra;
            OnMudraDetected.Invoke(side + ": " + currentMudras[hand]);
        }
    }
}