using UnityEngine;
using UnityEngine.UI;

public class PackageItem : MonoBehaviour
{
    [Header("Inspect Window")]
    public Sprite inspectSprite;

    [Header("Menu")]
    public Image sortedMenuImage;

    [Header("Held Item UI")]
    [Tooltip("Falls back to Inspect Sprite when left empty.")]
    public Sprite heldSprite;

    [TextArea(2, 5)]
    public string infoText;

    [TextArea(2, 5)]
    public string npcDialogueText;

    [TextArea(2, 5)]
    public string alternateNpcDialogueText;

    public BoxDropZone targetBox;

    [Header("Hold Pose (local to hold point)")]
    public Vector3 holdLocalPosition;
    public Vector3 holdLocalEuler;

    Rigidbody cachedRigidbody;
    Collider[] cachedColliders;
    Renderer[] cachedRenderers;
    bool[] cachedRendererStates;
    bool cachedKinematic;

    public bool IsHeld { get; private set; }
    public Sprite InspectSprite => inspectSprite;
    public Sprite HeldSprite => heldSprite != null ? heldSprite : inspectSprite;
    public string InfoText => infoText;
    public string NpcDialogueText => npcDialogueText;
    public string AlternateNpcDialogueText => alternateNpcDialogueText;

    public string GetNpcDialogueText(bool useAlternate)
    {
        if (useAlternate && !string.IsNullOrWhiteSpace(alternateNpcDialogueText))
            return alternateNpcDialogueText;

        return npcDialogueText;
    }

    public void RevealInMenu()
    {
        if (sortedMenuImage == null || inspectSprite == null)
            return;

        sortedMenuImage.sprite = inspectSprite;
        sortedMenuImage.preserveAspect = true;
        sortedMenuImage.color = Color.white;
        sortedMenuImage.enabled = true;
    }

    void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        cachedColliders = GetComponentsInChildren<Collider>();
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedRendererStates = new bool[cachedRenderers.Length];

        for (int i = 0; i < cachedRenderers.Length; i++)
            cachedRendererStates[i] = cachedRenderers[i] != null && cachedRenderers[i].enabled;
    }

    public void PickUp(Transform holdPoint)
    {
        if (IsHeld || holdPoint == null)
            return;

        IsHeld = true;

        if (cachedRigidbody != null)
        {
            cachedKinematic = cachedRigidbody.isKinematic;
            if (!cachedRigidbody.isKinematic)
            {
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.isKinematic = true;
            }
        }

        SetCollidersEnabled(false);
        SetRenderersEnabled(false);

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
        RestoreRendererStates();

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
            if (!cachedRigidbody.isKinematic)
            {
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.isKinematic = true;
            }
        }

        SetCollidersEnabled(false);
        SetRenderersEnabled(false);
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

    void SetRenderersEnabled(bool enabled)
    {
        if (cachedRenderers == null)
            return;

        foreach (var renderer in cachedRenderers)
        {
            if (renderer != null)
                renderer.enabled = enabled;
        }
    }

    void RestoreRendererStates()
    {
        if (cachedRenderers == null || cachedRendererStates == null)
            return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
                cachedRenderers[i].enabled = cachedRendererStates[i];
        }
    }
}
