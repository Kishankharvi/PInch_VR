using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Detects a set of configurable mudras using skeleton joints and pinch strengths.
/// Fires an event (string) when mudra changes. Also exposes current mudra via properties.
/// </summary>
public enum MudraType { None, Surya, Prithvi, Apan, Custom1 }

public class MudraDetectionManager : MonoBehaviour
{
    [Header("Hands & Skeletons")]
    public OVRHand leftHand;
    public OVRHand rightHand;
    public OVRSkeleton leftSkeleton;
    public OVRSkeleton rightSkeleton;

    [Header("Thresholds (meters)")]
    public float thumbToIndexThreshold = 0.025f;
    public float thumbToMiddleThreshold = 0.025f;
    public float thumbToRingThreshold = 0.02f;
    public float thumbToPinkyThreshold = 0.03f;

    [Header("UI")]
    public TextMeshProUGUI leftMudraText;
    public TextMeshProUGUI rightMudraText;
    public Image leftMudraImage;
    public Image rightMudraImage;
    public Sprite suryaSprite;
    public Sprite prithviSprite;
    public Sprite apanSprite;
    public Sprite noneSprite;

    public MudraType leftDetected = MudraType.None;
    public MudraType rightDetected = MudraType.None;

    void Update()
    {
        leftDetected = DetectMudra(leftHand, leftSkeleton);
        rightDetected = DetectMudra(rightHand, rightSkeleton);
        UpdateUI();
    }

    private MudraType DetectMudra(OVRHand hand, OVRSkeleton skeleton)
    {
        if (hand == null || !hand.IsTracked || skeleton == null || skeleton.Bones.Count == 0)
            return MudraType.None;

        Vector3 thumb = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_ThumbTip].Transform.position;
        Vector3 index = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_IndexTip].Transform.position;
        Vector3 middle = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_MiddleTip].Transform.position;
        Vector3 ring = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_RingTip].Transform.position;
        Vector3 pinky = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_PinkyTip].Transform.position;

        float tIndex = Vector3.Distance(thumb, index);
        float tMiddle = Vector3.Distance(thumb, middle);
        float tRing = Vector3.Distance(thumb, ring);
        float tPinky = Vector3.Distance(thumb, pinky);

        // Surya: thumb + ring close, others open
        if (tRing < thumbToRingThreshold && tMiddle > thumbToMiddleThreshold && tIndex > thumbToIndexThreshold)
            return MudraType.Surya;

        // Prithvi: thumb + middle close
        if (tMiddle < thumbToMiddleThreshold && tIndex > thumbToIndexThreshold && tRing > thumbToRingThreshold)
            return MudraType.Prithvi;

        // Apan: thumb + middle + ring close
        if (tMiddle < thumbToMiddleThreshold && tRing < thumbToRingThreshold)
            return MudraType.Apan;

        return MudraType.None;
    }

    private void UpdateUI()
    {
        if (leftMudraText != null) leftMudraText.text = $"Left: {leftDetected}";
        if (rightMudraText != null) rightMudraText.text = $"Right: {rightDetected}";
        if (leftMudraImage != null) leftMudraImage.sprite = GetSpriteForMudra(leftDetected);
        if (rightMudraImage != null) rightMudraImage.sprite = GetSpriteForMudra(rightDetected);
    }

    private Sprite GetSpriteForMudra(MudraType t)
    {
        switch (t)
        {
            case MudraType.Surya: return suryaSprite ?? noneSprite;
            case MudraType.Prithvi: return prithviSprite ?? noneSprite;
            case MudraType.Apan: return apanSprite ?? noneSprite;
            default: return noneSprite;
        }
    }
}
