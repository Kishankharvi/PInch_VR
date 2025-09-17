using UnityEngine;

public class PinchVisualizer : MonoBehaviour
{
    public OVRHAndsPinch pinchController; // Assign your GameManager here
    public OVRSkeleton handSkeleton;      // Assign the hand this script is for
    public Material defaultMaterial;
    public Material pinchMaterial;

    private Renderer[] fingerTipRenderers;

    // These are the bone IDs for the fingertips in OVRSkeleton
    private readonly OVRSkeleton.BoneId[] fingerTipIds =
    {
        OVRSkeleton.BoneId.Hand_ThumbTip,
        OVRSkeleton.BoneId.Hand_IndexTip,
        OVRSkeleton.BoneId.Hand_MiddleTip,
        OVRSkeleton.BoneId.Hand_RingTip,
        OVRSkeleton.BoneId.Hand_PinkyTip
    };

    void Start()
    {
        // Find the renderers for each fingertip
        fingerTipRenderers = new Renderer[fingerTipIds.Length];
        for (int i = 0; i < fingerTipIds.Length; i++)
        {
            Transform boneTransform = handSkeleton.Bones[(int)fingerTipIds[i]].Transform;
            if (boneTransform && boneTransform.GetChild(0) != null)
            {
                fingerTipRenderers[i] = boneTransform.GetChild(0).GetComponent<Renderer>();
            }
        }

        // Subscribe to the pinch events from your main script
        pinchController.OnPinchStart.AddListener(HandlePinchStart);
        pinchController.OnPinchEnd.AddListener(HandlePinchEnd);
    }

    private void HandlePinchStart(OVRHand hand, int fingerIndex, float strength)
    {
        if (handSkeleton != null && handSkeleton.GetComponent<OVRHand>() == hand)
        {
            SetMaterial(fingerIndex, pinchMaterial);
        }
    }

    private void HandlePinchEnd(OVRHand hand, int fingerIndex, float strength)
    {
        if (handSkeleton != null && handSkeleton.GetComponent<OVRHand>() == hand)
        {
            SetMaterial(fingerIndex, defaultMaterial);
        }
    }

    private void SetMaterial(int fingerIndex, Material mat)
    {
        if (fingerIndex >= 0 && fingerIndex < fingerTipRenderers.Length && fingerTipRenderers[fingerIndex] != null)
        {
            fingerTipRenderers[fingerIndex].material = mat;
        }
    }
}
