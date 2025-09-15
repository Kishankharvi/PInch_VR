using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PinchUiManager : MonoBehaviour
{
    [Header("Sliders: Index, Middle, Ring, Pinky")]
    public Slider[] fingerSliders; // length 4
    public TextMeshProUGUI[] fingerValueTexts; // numeric labels next to sliders
    public TextMeshProUGUI statusText;
    public Image mudraImage;
    public Sprite noneSprite;

    // nice progress bar color change (optional)
    public Image[] sliderFillImages;

    void Start()
    {
        if (fingerSliders == null || fingerSliders.Length < 4)
            Debug.LogWarning("Assign 4 finger sliders in inspector");
    }

    // This method exactly matches event signature (OVRHand, int, float)
    public void UpdateFingerStrength(OVRHand hand, int fingerIndex, float strength)
    {
        if (fingerIndex < 0 || fingerIndex >= fingerSliders.Length) return;
        float v = Mathf.Clamp01(strength);
        fingerSliders[fingerIndex].value = v;
        if (fingerValueTexts != null && fingerValueTexts.Length > fingerIndex)
            fingerValueTexts[fingerIndex].text = (v * 100f).ToString("0") + "%";

        // color change based on strength
        if (sliderFillImages != null && fingerIndex < sliderFillImages.Length)
        {
            var img = sliderFillImages[fingerIndex];
            if (img != null) img.color = Color.Lerp(Color.green, Color.red, v);
        }
    }

    // convenience overload for hooking simple text (string) events
    public void UpdateStatusText(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    // alternative signature in case someone hooks (int,float)
    public void UpdateFingerStrengthSimple(int fingerIndex, float strength)
    {
        UpdateFingerStrength(null, fingerIndex, strength);
    }

    public void SetMudraImage(Sprite s)
    {
        if (mudraImage != null) mudraImage.sprite = s ?? noneSprite;
    }
}
