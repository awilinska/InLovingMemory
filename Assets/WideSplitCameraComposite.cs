using UnityEngine;

[DefaultExecutionOrder(10000)] // run late (after ProCamera2D / Cinemachine / your follow scripts)
public class WideSplitCameraComposite : MonoBehaviour
{
    [Header("Master camera that defines the ONE virtual view")]
    [Tooltip("This camera defines position/zoom/FOV. It can be a ProCamera2D-driven camera.")]
    public Camera master;

    [Header("Render cameras (no ProCamera2D/Cinemachine on these)")]
    public Camera leftCam;
    public Camera rightCam;

    [Header("Seam")]
    [Range(0.05f, 0.95f)]
    [Tooltip("Portion of the screen taken by the LEFT camera. 0.66 = left takes 2/3, right takes 1/3.")]
    public float seam = 0.5f;

    [Tooltip("Keep projection matrices updated every frame (recommended if seam can change at runtime).")]
    public bool updateEveryFrame = true;

    void OnEnable() => Apply();

    void LateUpdate()
    {
        if (updateEveryFrame) Apply();
    }

#if UNITY_EDITOR
    // So dragging the seam slider in editor updates immediately
    void OnValidate()
    {
        seam = Mathf.Clamp(seam, 0.05f, 0.95f);
        Apply();
    }
#endif

    public void Apply()
    {
        if (!master || !leftCam || !rightCam) return;

        // Pose + core settings identical -> one continuous virtual view
        SyncCameraPoseAndSettings(master, leftCam);
        SyncCameraPoseAndSettings(master, rightCam);

        // Viewport split
        leftCam.rect  = new Rect(0f,   0f, seam,      1f);
        rightCam.rect = new Rect(seam, 0f, 1f - seam, 1f);

        // Split ONE wide frustum at seam
        ApplySplitProjection(master, leftCam,  isLeft: true,  seam01: seam);
        ApplySplitProjection(master, rightCam, isLeft: false, seam01: seam);
    }

    static void SyncCameraPoseAndSettings(Camera src, Camera dst)
    {
        dst.transform.position = src.transform.position;
        dst.transform.rotation = src.transform.rotation;

        dst.orthographic   = src.orthographic;
        dst.nearClipPlane  = src.nearClipPlane;
        dst.farClipPlane   = src.farClipPlane;

        if (src.orthographic) dst.orthographicSize = src.orthographicSize;
        else                  dst.fieldOfView      = src.fieldOfView;

        dst.clearFlags      = src.clearFlags;
        dst.backgroundColor = src.backgroundColor;

        // If you use physical camera settings etc., copy those too if needed.
    }

    static void ApplySplitProjection(Camera master, Camera dst, bool isLeft, float seam01)
    {
        // Compute the FULL virtual view using full screen aspect,
        // then split it at seam01 into left/right halves.
        float fullAspect = (float)Screen.width / Mathf.Max(1f, Screen.height);

        float n = master.nearClipPlane;
        float f = master.farClipPlane;

        if (master.orthographic)
        {
            float top = master.orthographicSize;
            float bottom = -top;
            float rFull = top * fullAspect;
            float lFull = -rFull;

            float mid = Mathf.Lerp(lFull, rFull, seam01);

            float l = isLeft ? lFull : mid;
            float r = isLeft ? mid   : rFull;

            dst.projectionMatrix = Matrix4x4.Ortho(l, r, bottom, top, n, f);
        }
        else
        {
            float top = n * Mathf.Tan(master.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float bottom = -top;
            float rFull = top * fullAspect;
            float lFull = -rFull;

            float mid = Mathf.Lerp(lFull, rFull, seam01);

            float l = isLeft ? lFull : mid;
            float r = isLeft ? mid   : rFull;

            dst.projectionMatrix = Matrix4x4.Frustum(l, r, bottom, top, n, f);
        }
    }
}
