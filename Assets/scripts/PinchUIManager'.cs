using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PinchUIManager : MonoBehaviour
{
    public Slider[] fingerSliders; // 0=index, 1=middle, 2=ring, 3=pinky
    public TextMeshProUGUI statusText;

    // Match event signature exactly: (OVRHand, int, float)
    public void UpdateFingerStrength(OVRHand hand, int fingerIndex, float strength)
    {
        if (fingerIndex >= 0 && fingerIndex < fingerSliders.Length)
            fingerSliders[fingerIndex].value = strength;
    }

    public void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
