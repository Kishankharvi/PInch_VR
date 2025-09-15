using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to a GameObject to visualize the given OVRSkeleton as lines between bone positions.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SkeletonVisualizer : MonoBehaviour
{
    public OVRSkeleton skeleton;
    public Color lineColor = Color.cyan;
    public float lineWidth = 0.005f;

    private LineRenderer lr;
    private List<Vector3> points = new List<Vector3>();

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.startWidth = lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = 0;
        lr.loop = false;
        lr.startColor = lr.endColor = lineColor;
    }

    void Update()
    {
        if (skeleton == null || skeleton.Bones == null || skeleton.Bones.Count == 0)
        {
            lr.positionCount = 0;
            return;
        }

        points.Clear();
        // Draw a simple chain from wrist to each tip: wrist -> palm -> each finger chain tip
        Transform wrist = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_WristRoot].Transform;
        if (wrist == null) return;

        // simple approach: draw lines from wrist to each tip
        Vector3 wristPos = wrist.position;
        var tips = new OVRSkeleton.BoneId[] {
            OVRSkeleton.BoneId.Hand_IndexTip,
            OVRSkeleton.BoneId.Hand_MiddleTip,
            OVRSkeleton.BoneId.Hand_RingTip,
            OVRSkeleton.BoneId.Hand_PinkyTip,
            OVRSkeleton.BoneId.Hand_ThumbTip
        };

        points.Add(wristPos);
        foreach (var t in tips)
        {
            points.Add(skeleton.Bones[(int)t].Transform.position);
            points.Add(wristPos); // back to wrist for fan lines
        }

        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
    }
}
