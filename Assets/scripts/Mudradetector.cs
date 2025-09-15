using UnityEngine;
using UnityEngine.Events;

public class MudraDetector : MonoBehaviour
{
    public OVRHand leftHand;
    public OVRHand rightHand;

    [System.Serializable]
    public class MudraEvent : UnityEvent<string> { }
    public MudraEvent OnMudraDetected;

    private string currentMudra = "None";

    void Update()
    {
        DetectMudra(leftHand, "Left");
        DetectMudra(rightHand, "Right");
    }

    void DetectMudra(OVRHand hand, string side)
    {
        if (hand == null) return;

        // Example Mudra: Surya Mudra - Thumb + Ring
        bool isThumb = hand.GetFingerIsPinching(OVRHand.HandFinger.Thumb);
        bool isRing = hand.GetFingerIsPinching(OVRHand.HandFinger.Ring);

        string detectedMudra = "None";
        if (isThumb && isRing)
            detectedMudra = "Surya Mudra";

        if (detectedMudra != currentMudra)
        {
            currentMudra = detectedMudra;
            OnMudraDetected.Invoke(side + ": " + currentMudra);
        }
    }
}
