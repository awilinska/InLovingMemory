using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerPackageInteractor : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text infoText;
    public GameObject infoPanel;
    public Image infoImage;
    public Image heldPackageImage;
    public GameObject packageInteractionInfo;
    public GameObject leaveItem;
    public GameObject conversationStarter;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public GameObject menu;
    
    [Header("World Prompts (3D TextMeshPro)")]
    public TextMeshPro packagePrompt3D;
    public Vector3 packagePromptOffset = new Vector3(0f, 1.5f, 0f);
    public TextMeshPro npcPrompt3D;
    public Vector3 npcPromptOffset = new Vector3(0f, 1.8f, 0f);
    public bool billboardWorldPrompts = true;

    [TextArea(2, 5)]
    public string[] dialogueLines;
    [TextArea(2, 5)]
    public string[] wrongBoxDialogueLines =
    {
        "This doesn't belong in this box.",
        "I should put this somewhere else.",
        "No, this is the wrong box."
    };
    [Header("Counter")]
    public TMP_Text counterText;
    public int totalPackages;

    [Header("Hold")]
    public Transform holdPoint;

    [Header("Tags")]
    public string packageTag = "Package";
    public string boxTag = "Box";
    public string npcTag = "NPC";

    [Header("Keys")]
    public Key pickupDropKey = Key.F;
    public Key infoKey = Key.E;
    public Key talkKey = Key.T;
    public Key closePanelKey = Key.X;
    public Key closeDialogueKey = Key.Space;
    public Key menuKey = Key.Tab;

    PackageItem nearbyPackage;
    BoxDropZone nearbyBox;
    PackageItem heldPackage;
    Transform nearbyNpc;
    int deliveredCount;
    bool isInNpcRange;
    Camera promptCamera;
    Material packagePromptOverlayMaterial;
    Material npcPromptOverlayMaterial;
    TMP_FontAsset packagePromptFontAsset;
    TMP_FontAsset npcPromptFontAsset;
    bool packageInteractionEnabled = true;
    bool packageCompletionReported;

    public event Func<bool> NpcTalkRequested;
    public event Action<int, int> PackageProgressChanged;
    public event Action PackagesCompleted;

    public bool PackageInteractionEnabled => packageInteractionEnabled;
    public int DeliveredPackageCount => deliveredCount;
    public int TotalPackageCount => totalPackages;

    void Start()
    {
        promptCamera = Camera.main;
        if (menu != null)
            menu.SetActive(false);

        ResolveInfoImage();
        ShowHeldPackageSprite(null);
        UpdateDropPrompt();
        UpdateCounter();
        EnsurePromptRendering(
            packagePrompt3D,
            ref packagePromptOverlayMaterial,
            ref packagePromptFontAsset);
        EnsurePromptRendering(
            npcPrompt3D,
            ref npcPromptOverlayMaterial,
            ref npcPromptFontAsset);
        SetPromptActive(packagePrompt3D, false);
        SetPromptActive(npcPrompt3D, false);
        if (packagePrompt3D != null && packageInteractionInfo != null)
            packageInteractionInfo.SetActive(false);
        if (npcPrompt3D != null && conversationStarter != null)
            conversationStarter.SetActive(false);
    }

    void OnDestroy()
    {
        Destroy(packagePromptOverlayMaterial);
        Destroy(npcPromptOverlayMaterial);
    }

    void Update()
    {
        if (WasPressed(menuKey))
            ToggleMenu();

        if (WasPressed(infoKey))
            ShowInfo();

        if (WasPressed(pickupDropKey))
            HandlePickupDrop();

        if (WasPressed(talkKey))
            TryTalk();

        if (WasPressed(closePanelKey))
            ClosePanels();

        if (WasPressed(closeDialogueKey))
            CloseDialoguePanel();

        UpdateWorldPrompts();
    }

    void ToggleMenu()
    {
        if (menu != null)
            menu.SetActive(!menu.activeSelf);
    }

    bool WasPressed(Key key)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return keyboard[key].wasPressedThisFrame;
    }

    void ShowInfo()
    {
        if (!packageInteractionEnabled)
            return;

        var target = heldPackage != null ? heldPackage : nearbyPackage;
        if (target == null || infoText == null)
            return;

        if (infoPanel != null)
            infoPanel.SetActive(true);

        infoText.text = target.InfoText;
        ShowInfoSprite(target.InspectSprite);
    }

    void ResolveInfoImage()
    {
        if (infoImage != null || infoPanel == null)
            return;

        var images = infoPanel.GetComponentsInChildren<Image>(true);
        foreach (var image in images)
        {
            if (image != null && image.gameObject != infoPanel && image.gameObject.name == "Image")
            {
                infoImage = image;
                break;
            }
        }
    }

    void ShowInfoSprite(Sprite sprite)
    {
        ResolveInfoImage();
        if (infoImage == null)
            return;

        infoImage.sprite = sprite;
        infoImage.preserveAspect = true;
        infoImage.gameObject.SetActive(sprite != null);
    }

    void ShowHeldPackageSprite(Sprite sprite)
    {
        if (heldPackageImage == null)
            return;

        heldPackageImage.sprite = sprite;
        heldPackageImage.preserveAspect = true;
        heldPackageImage.gameObject.SetActive(sprite != null);
    }

    void HandlePickupDrop()
    {
        if (!packageInteractionEnabled)
            return;

        if (heldPackage == null)
            TryPickUp();
        else
            TryDrop();
    }

    void TryPickUp()
    {
        if (nearbyPackage == null)
            return;

        heldPackage = nearbyPackage;
        nearbyPackage = null;

        var parent = holdPoint != null ? holdPoint : transform;
        heldPackage.PickUp(parent);
        ShowHeldPackageSprite(heldPackage.HeldSprite);

        if (packageInteractionInfo != null)
            packageInteractionInfo.SetActive(false);

        UpdateDropPrompt();
    }

    void TryDrop()
    {
        if (nearbyBox == null)
            return;

        if (heldPackage.targetBox != null && heldPackage.targetBox != nearbyBox)
        {
            ShowRandomDialogue(wrongBoxDialogueLines);
            return;
        }

        CompleteDelivery(nearbyBox);
    }

    void TryTalk()
    {
        if (!isInNpcRange || dialogueText == null)
            return;

        if (heldPackage != null && !string.IsNullOrWhiteSpace(heldPackage.NpcDialogueText))
        {
            ShowDialogueMessage(heldPackage.NpcDialogueText);
            return;
        }

        if (HandleNpcTalkRequest())
            return;

        ShowRandomDialogue(dialogueLines);
    }

    bool HandleNpcTalkRequest()
    {
        if (NpcTalkRequested == null)
            return false;

        foreach (Func<bool> handler in NpcTalkRequested.GetInvocationList())
        {
            if (handler())
                return true;
        }

        return false;
    }

    void ShowRandomDialogue(string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return;

        var index = UnityEngine.Random.Range(0, lines.Length);
        ShowDialogueMessage(lines[index]);
    }

    void CompleteDelivery(BoxDropZone dropZone)
    {
        var dropPoint = dropZone.GetDropPoint();
        var delivered = heldPackage;
        delivered.DeliverTo(dropPoint, dropZone.transform);
        delivered.RevealInMenu();

        heldPackage = null;
        ShowHeldPackageSprite(null);
        deliveredCount++;
        UpdateCounter();
        PackageProgressChanged?.Invoke(deliveredCount, totalPackages);

        Destroy(delivered.gameObject);

        UpdateDropPrompt();

        if (!packageCompletionReported && totalPackages > 0 && deliveredCount >= totalPackages)
        {
            packageCompletionReported = true;
            PackagesCompleted?.Invoke();
        }
    }

    void UpdateCounter()
    {
        if (counterText == null)
            return;

        counterText.text = $"{deliveredCount}/{totalPackages}";
    }

    public void ShowDialogueMessage(string message)
    {
        if (dialogueText == null)
            return;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        dialogueText.text = message;
    }

    public void SetPackageInteractionEnabled(bool enabled)
    {
        packageInteractionEnabled = enabled;

        if (!enabled)
        {
            if (packageInteractionInfo != null)
                packageInteractionInfo.SetActive(false);

            SetPromptActive(packagePrompt3D, false);
        }

        UpdateDropPrompt();
    }

    void ClosePanels()
    {
        if (infoPanel != null && infoPanel.activeSelf)
            infoPanel.SetActive(false);

        CloseDialoguePanel();
    }

    void CloseDialoguePanel()
    {
        if (dialoguePanel != null && dialoguePanel.activeSelf)
            dialoguePanel.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        var package = GetTaggedComponentInParents<PackageItem>(other, packageTag);
        if (package != null)
        {
            nearbyPackage = package;
            if (packageInteractionEnabled && packagePrompt3D == null && packageInteractionInfo != null)
                packageInteractionInfo.SetActive(true);
        }

        var box = GetTaggedComponentInParents<BoxDropZone>(other, boxTag);
        if (box != null)
        {
            nearbyBox = box;
            UpdateDropPrompt();
        }

        if (other.CompareTag(npcTag))
        {
            isInNpcRange = true;
            nearbyNpc = GetPromptTarget(other);
            if (npcPrompt3D == null && conversationStarter != null)
                conversationStarter.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        var package = GetTaggedComponentInParents<PackageItem>(other, packageTag);
        if (package != null)
        {
            if (nearbyPackage == package)
            {
                nearbyPackage = null;
                if (packagePrompt3D == null && packageInteractionInfo != null)
                    packageInteractionInfo.SetActive(false);
            }
        }

        var box = GetTaggedComponentInParents<BoxDropZone>(other, boxTag);
        if (box != null)
        {
            if (nearbyBox == box)
            {
                nearbyBox = null;
                UpdateDropPrompt();
            }
        }

        if (other.CompareTag(npcTag))
        {
            isInNpcRange = false;
            nearbyNpc = null;
            if (npcPrompt3D == null && conversationStarter != null)
                conversationStarter.SetActive(false);
        }
    }

    void UpdateWorldPrompts()
    {
        if (packagePrompt3D != null)
        {
            bool showPackagePrompt =
                packageInteractionEnabled && nearbyPackage != null && heldPackage == null;
            bool wasActivated = SetPromptActive(packagePrompt3D, showPackagePrompt);
            if (showPackagePrompt)
            {
                if (wasActivated)
                    packagePrompt3D.ForceMeshUpdate(true, true);

                EnsurePromptRendering(
                    packagePrompt3D,
                    ref packagePromptOverlayMaterial,
                    ref packagePromptFontAsset);
                PositionPrompt(packagePrompt3D.transform, nearbyPackage.transform, packagePromptOffset);
            }
        }

        if (npcPrompt3D != null)
        {
            bool showNpcPrompt = isInNpcRange && nearbyNpc != null;
            bool wasActivated = SetPromptActive(npcPrompt3D, showNpcPrompt);
            if (showNpcPrompt)
            {
                if (wasActivated)
                    npcPrompt3D.ForceMeshUpdate(true, true);

                EnsurePromptRendering(
                    npcPrompt3D,
                    ref npcPromptOverlayMaterial,
                    ref npcPromptFontAsset);
                PositionPrompt(npcPrompt3D.transform, nearbyNpc, npcPromptOffset);
            }
        }

        if (leaveItem != null && leaveItem.activeInHierarchy)
            FaceCamera(leaveItem.transform);
    }

    void UpdateDropPrompt()
    {
        if (leaveItem != null)
        {
            leaveItem.SetActive(
                packageInteractionEnabled && heldPackage != null && nearbyBox != null);
        }
    }

    void PositionPrompt(Transform prompt, Transform target, Vector3 offset)
    {
        if (prompt == null || target == null)
            return;

        prompt.position = target.position + offset;

        if (!billboardWorldPrompts)
            return;

        if (promptCamera == null)
            promptCamera = Camera.main;

        if (promptCamera == null)
            return;

        FaceCamera(prompt);
    }

    void FaceCamera(Transform target)
    {
        if (target == null)
            return;

        if (promptCamera == null)
            promptCamera = Camera.main;

        if (promptCamera == null)
            return;

        var toCamera = promptCamera.transform.position - target.position;
        if (toCamera.sqrMagnitude <= 0.0001f)
            return;

        // TMP 3D text front face is opposite in this setup, so invert the billboard forward vector.
        target.rotation = Quaternion.LookRotation((-toCamera).normalized, Vector3.up);
    }

    static bool SetPromptActive(TMP_Text prompt, bool active)
    {
        if (prompt == null || prompt.gameObject.activeSelf == active)
            return false;

        prompt.gameObject.SetActive(active);
        return active;
    }

    static Transform GetPromptTarget(Collider other)
    {
        if (other == null)
            return null;

        if (other.attachedRigidbody != null)
            return other.attachedRigidbody.transform;

        return other.transform;
    }

    static T GetTaggedComponentInParents<T>(Collider other, string requiredTag)
        where T : Component
    {
        if (other == null)
            return null;

        var component = other.GetComponentInParent<T>();
        if (component == null)
            return null;

        for (Transform current = other.transform; current != null; current = current.parent)
        {
            if (current.CompareTag(requiredTag))
                return component;
        }

        return null;
    }

    static void EnsurePromptRendering(
        TextMeshPro prompt,
        ref Material overlayMaterial,
        ref TMP_FontAsset configuredFontAsset)
    {
        if (prompt == null)
            return;

        var renderer = prompt.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sortingOrder = short.MaxValue;

        var fontAsset = prompt.font;
        var overlayShader = Shader.Find("TextMeshPro/Distance Field Overlay");
        if (fontAsset == null || overlayShader == null)
            return;

        bool needsRebuild =
            overlayMaterial == null
            || configuredFontAsset != fontAsset
            || prompt.fontSharedMaterial != overlayMaterial
            || overlayMaterial.shader != overlayShader;

        if (!needsRebuild)
            return;

        var sourceMaterial = prompt.fontSharedMaterial;
        if (sourceMaterial == null
            || sourceMaterial == overlayMaterial
            || sourceMaterial.mainTexture != fontAsset.atlasTexture)
        {
            sourceMaterial = fontAsset.material;
        }

        if (sourceMaterial == null)
            return;

        if (overlayMaterial != null)
            Destroy(overlayMaterial);

        // TMP's overlay shader disables depth testing, matching screen-space UI behavior.
        overlayMaterial = new Material(sourceMaterial)
        {
            name = $"{sourceMaterial.name} (World Prompt Overlay)",
            shader = overlayShader,
            renderQueue = 5000
        };

        prompt.fontMaterial = overlayMaterial;
        prompt.SetMaterialDirty();
        configuredFontAsset = fontAsset;
    }
}
