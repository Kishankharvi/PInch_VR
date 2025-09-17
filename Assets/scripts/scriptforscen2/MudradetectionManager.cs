using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Improved mudra detector using OVRSkeleton bone tip distances.
/// - Safe bone lookups
/// - Optional auto-normalization by hand size
/// - Runtime debug text to tune thresholds
/// </summary>
public enum MudraType { None, Surya, Prithvi, Apan, Custom1 }

public class MudraDetectionManager : MonoBehaviour
{
    [Header("Hands & Skeletons (assign in Inspector)")]
    public OVRHand leftHand;
    public OVRHand rightHand;
    public OVRSkeleton leftSkeleton;
    public OVRSkeleton rightSkeleton;

    [Header("Thresholds (meters) — tune these)")]
    [Tooltip("If autoNormalize = true these are treated as relative reference distances")]
    public float thumbToIndexThreshold = 0.04f;
    public float thumbToMiddleThreshold = 0.035f;
    public float thumbToRingThreshold = 0.03f;
    public float thumbToPinkyThreshold = 0.04f;

    [Header("Pinch (for normalized pinch value)")]
    [Tooltip("Distance at which pinch is considered '0' (open). Smaller distances -> stronger pinch.")]
    public float pinchMaxDistance = 0.06f; // reasonable starting value
    public float pinchMinDistance = 0.015f; // distance treated as full pinch (1.0)

    [Header("Auto-normalize / scaling")]
    [Tooltip("If true, thresholds will scale by hand size to work across different users/avatars")]
    public bool autoNormalize = true;
    [Tooltip("Reference hand length (meters) assumed when thresholds were tuned. e.g. 0.10 = 10cm")]
    public float handReferenceLength = 0.10f;

    [Header("UI (optional)")]
    public TextMeshProUGUI leftMudraText;
    public TextMeshProUGUI rightMudraText;
    public Image leftMudraImage;
    public Image rightMudraImage;
    public Sprite suryaSprite;
    public Sprite prithviSprite;
    public Sprite apanSprite;
    public Sprite noneSprite;

    [Header("Debug (optional)")]
    public bool verboseDebug = false;
    public TextMeshProUGUI leftDebugText;   // will show distances & pinch value
    public TextMeshProUGUI rightDebugText;

    [HideInInspector] public MudraType leftDetected = MudraType.None;
    [HideInInspector] public MudraType rightDetected = MudraType.None;

    void Update()
    {
        string leftDbg = "";
        string rightDbg = "";

        leftDetected = DetectMudra(leftHand, leftSkeleton, out leftDbg);
        rightDetected = DetectMudra(rightHand, rightSkeleton, out rightDbg);

        UpdateUI(leftDbg, rightDbg);
    }

    /// <summary>
    /// Detect mudra using thumb-tip distances to other finger tips.
    /// Returns a debug string showing raw distances and scaled thresholds (useful for tuning).
    /// </summary>
    private MudraType DetectMudra(OVRHand hand, OVRSkeleton skeleton, out string debug)
    {
        debug = "";

        if (hand == null || skeleton == null)
        {
            debug = "No hand/skeleton assigned";
            return MudraType.None;
        }

        // If not tracked (OVRHand.IsTracked) we bail early
        if (!hand.IsTracked)
        {
            debug = "Hand not tracked (OVRHand.IsTracked == false)";
            return MudraType.None;
        }

        // Require valid bone list
        if (skeleton.Bones == null || skeleton.Bones.Count == 0)
        {
            debug = "Skeleton bones empty (waiting for initialization)";
            return MudraType.None;
        }

        // safely fetch tip positions
        if (!TryGetBonePosition(skeleton, OVRSkeleton.BoneId.Hand_ThumbTip, out Vector3 thumb)
            || !TryGetBonePosition(skeleton, OVRSkeleton.BoneId.Hand_IndexTip, out Vector3 index)
            || !TryGetBonePosition(skeleton, OVRSkeleton.BoneId.Hand_MiddleTip, out Vector3 middle)
            || !TryGetBonePosition(skeleton, OVRSkeleton.BoneId.Hand_RingTip, out Vector3 ring)
            || !TryGetBonePosition(skeleton, OVRSkeleton.BoneId.Hand_PinkyTip, out Vector3 pinky))
        {
            debug = "One or more tip bones missing";
            return MudraType.None;
        }

        // compute raw distances
        float dIndex = Vector3.Distance(thumb, index);
        float dMiddle = Vector3.Distance(thumb, middle);
        float dRing = Vector3.Distance(thumb, ring);
        float dPinky = Vector3.Distance(thumb, pinky);

        // compute scale multiplier using hand size if enabled
        float scaleMultiplier = 1f;
        if (autoNormalize)
        {
            // try to compute a hand length reference (wrist -> middle tip) if available
            if (TryGetBonePosition(skeleton, OVRSkeleton.BoneId.Hand_WristRoot, out Vector3 wrist)
                && TryGetBonePosition(skeleton, OVRSkeleton.BoneId.Hand_MiddleTip, out Vector3 middleTipForScale))
            {
                float handLen = Vector3.Distance(wrist, middleTipForScale);
                if (handLen > 0.001f) scaleMultiplier = handLen / handReferenceLength;
            }
            // else we leave scaleMultiplier = 1
        }

        // scaled thresholds
        float tIndexTh = thumbToIndexThreshold * scaleMultiplier;
        float tMiddleTh = thumbToMiddleThreshold * scaleMultiplier;
        float tRingTh = thumbToRingThreshold * scaleMultiplier;
        float tPinkyTh = thumbToPinkyThreshold * scaleMultiplier;

        // boolean closeness checks
        bool thumbIndexClose = dIndex <= tIndexTh;
        bool thumbMiddleClose = dMiddle <= tMiddleTh;
        bool thumbRingClose = dRing <= tRingTh;
        bool thumbPinkyClose = dPinky <= tPinkyTh;

        // pinch value (0..1) based on thumb-index distance (clamped).
        // Inverse-linterp with pinchMinDistance => full pinch, pinchMaxDistance => open
        float pinchValue = Mathf.InverseLerp(pinchMaxDistance, pinchMinDistance, dIndex);
        pinchValue = Mathf.Clamp01(pinchValue);

        // debug string
        debug = $"dI:{dIndex:F3}(th:{tIndexTh:F3}) dM:{dMiddle:F3}(th:{tMiddleTh:F3}) dR:{dRing:F3}(th:{tRingTh:F3}) dP:{dPinky:F3}(th:{tPinkyTh:F3}) pinch:{pinchValue:F2}";

        // Detection logic (order: Apan (strong), Surya, Prithvi)
        // Apan: thumb + middle + ring close
        if (thumbMiddleClose && thumbRingClose)
            return MudraType.Apan;

        // Surya: thumb + ring close, others not close
        if (thumbRingClose && !thumbMiddleClose && !thumbIndexClose)
            return MudraType.Surya;

        // Prithvi: thumb + middle close, others not close
        if (thumbMiddleClose && !thumbRingClose && !thumbIndexClose)
            return MudraType.Prithvi;

        return MudraType.None;
    }

    private void UpdateUI(string leftDebug, string rightDebug)
    {
        if (leftMudraText != null) leftMudraText.text = $"Left: {leftDetected}";
        if (rightMudraText != null) rightMudraText.text = $"Right: {rightDetected}";

        if (leftMudraImage != null) leftMudraImage.sprite = GetSpriteForMudra(leftDetected);
        if (rightMudraImage != null) rightMudraImage.sprite = GetSpriteForMudra(rightDetected);

        if (verboseDebug)
        {
            if (leftDebugText != null) leftDebugText.text = leftDebug;
            if (rightDebugText != null) rightDebugText.text = rightDebug;
        }
    }

    private Sprite GetSpriteForMudra(MudraType t)
    {
        switch (t)
        {
            case MudraType.Surya: return suryaSprite != null ? suryaSprite : noneSprite;
            case MudraType.Prithvi: return prithviSprite != null ? prithviSprite : noneSprite;
            case MudraType.Apan: return apanSprite != null ? apanSprite : noneSprite;
            default: return noneSprite;
        }
    }

    /// <summary>
    /// Safely fetch bone transform position. Returns false if index out-of-range or transform missing.
    /// </summary>
    private bool TryGetBonePosition(OVRSkeleton skeleton, OVRSkeleton.BoneId boneId, out Vector3 pos)
    {
        pos = Vector3.zero;
        if (skeleton == null || skeleton.Bones == null) return false;

        int idx = (int)boneId;
        if (idx < 0 || idx >= skeleton.Bones.Count) return false;

        var bone = skeleton.Bones[idx];
        if (bone == null || bone.Transform == null) return false;

        pos = bone.Transform.position;
        return true;
    }
}
