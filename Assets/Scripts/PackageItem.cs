using UnityEngine;

public class PackageItem : MonoBehaviour
{
    [TextArea(2, 5)]
    public string infoText;

    [TextArea(2, 5)]
    public string npcDialogueText;

    public BoxDropZone targetBox;

    [Header("Hold Pose (local to hold point)")]
    public Vector3 holdLocalPosition;
    public Vector3 holdLocalEuler;

    Rigidbody cachedRigidbody;
    Collider[] cachedColliders;
    bool cachedKinematic;

    public bool IsHeld { get; private set; }
    public string InfoText => infoText;
    public string NpcDialogueText => npcDialogueText;

    void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        cachedColliders = GetComponentsInChildren<Collider>();
    }

    public void PickUp(Transform holdPoint)
    {
        if (IsHeld || holdPoint == null)
            return;

        IsHeld = true;

        if (cachedRigidbody != null)
        {
            cachedKinematic = cachedRigidbody.isKinematic;
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
        }

        SetCollidersEnabled(false);

        transform.SetParent(holdPoint, false);
        transform.localPosition = holdLocalPosition;
        transform.localEulerAngles = holdLocalEuler;
    }

    public void Drop(Transform dropPoint, Transform newParent = null)
    {
        if (!IsHeld)
            return;

        IsHeld = false;

        transform.SetParent(newParent, true);

        if (dropPoint != null)
        {
            transform.position = dropPoint.position;
            transform.rotation = dropPoint.rotation;
        }

        SetCollidersEnabled(true);

        if (cachedRigidbody != null)
            cachedRigidbody.isKinematic = cachedKinematic;
    }

    public void DeliverTo(Transform dropPoint, Transform newParent = null)
    {
        IsHeld = false;

        transform.SetParent(newParent, true);

        if (dropPoint != null)
        {
            transform.position = dropPoint.position;
            transform.rotation = dropPoint.rotation;
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
        }

        SetCollidersEnabled(false);
    }

    void SetCollidersEnabled(bool enabled)
    {
        if (cachedColliders == null)
            return;

        foreach (var col in cachedColliders)
        {
            if (col != null)
                col.enabled = enabled;
        }
    }
}
