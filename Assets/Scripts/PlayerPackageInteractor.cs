using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPackageInteractor : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text infoText;
    public GameObject infoPanel;
    public GameObject packageInteractionInfo;
    public GameObject leaveItem;
    public GameObject conversationStarter;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    [TextArea(2, 5)]
    public string[] dialogueLines;
    [TextArea(2, 5)]
    public string startDialogueText = "We should pack the things to the boxes.";
    [TextArea(2, 5)]
    public string completionDialogueText = "Great job, we did it in no-time!";

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

    PackageItem nearbyPackage;
    BoxDropZone nearbyBox;
    PackageItem heldPackage;
    int deliveredCount;
    bool isInNpcRange;

    void Start()
    {
        UpdateCounter();
        ShowDialogueMessage(startDialogueText);
    }

    void Update()
    {
        if (WasPressed(infoKey))
            ShowInfo();

        if (WasPressed(pickupDropKey))
            HandlePickupDrop();

        if (WasPressed(talkKey))
            TryTalk();

        if (WasPressed(closePanelKey))
            ClosePanels();
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
        var target = heldPackage != null ? heldPackage : nearbyPackage;
        if (target == null || infoText == null)
            return;

        if (infoPanel != null)
            infoPanel.SetActive(true);

        infoText.text = target.InfoText;
    }

    void HandlePickupDrop()
    {
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

        if (packageInteractionInfo != null)
            packageInteractionInfo.SetActive(false);

        if (nearbyBox != null && leaveItem != null)
            leaveItem.SetActive(true);
    }

    void TryDrop()
    {
        if (nearbyBox == null)
            return;

        if (heldPackage.targetBox != null && heldPackage.targetBox != nearbyBox)
        {
            if (infoPanel != null)
                infoPanel.SetActive(true);

            if (infoText != null)
                infoText.text = "It don't belong here...";

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

        if (dialogueLines == null || dialogueLines.Length == 0)
            return;

        var index = Random.Range(0, dialogueLines.Length);
        ShowDialogueMessage(dialogueLines[index]);
    }

    void CompleteDelivery(BoxDropZone dropZone)
    {
        var dropPoint = dropZone.GetDropPoint();
        var delivered = heldPackage;
        delivered.DeliverTo(dropPoint, dropZone.transform);

        heldPackage = null;
        deliveredCount++;
        UpdateCounter();

        Destroy(delivered.gameObject);

        if (leaveItem != null)
            leaveItem.SetActive(false);

        if (totalPackages > 0 && deliveredCount >= totalPackages)
            ShowDialogueMessage(completionDialogueText);
    }

    void UpdateCounter()
    {
        if (counterText == null)
            return;

        counterText.text = $"{deliveredCount}/{totalPackages}";
    }

    void ShowDialogueMessage(string message)
    {
        if (dialogueText == null)
            return;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        dialogueText.text = message;
    }

    void ClosePanels()
    {
        if (infoPanel != null && infoPanel.activeSelf)
            infoPanel.SetActive(false);

        if (dialoguePanel != null && dialoguePanel.activeSelf)
            dialoguePanel.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(packageTag) && other.TryGetComponent(out PackageItem package))
        {
            nearbyPackage = package;
            if (packageInteractionInfo != null)
                packageInteractionInfo.SetActive(true);
        }

        if (other.CompareTag(boxTag) && other.TryGetComponent(out BoxDropZone box))
        {
            nearbyBox = box;
            if (heldPackage != null && leaveItem != null)
                leaveItem.SetActive(true);
        }

        if (other.CompareTag(npcTag))
        {
            isInNpcRange = true;
            if (conversationStarter != null)
                conversationStarter.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(packageTag) && other.TryGetComponent(out PackageItem package))
        {
            if (nearbyPackage == package)
            {
                nearbyPackage = null;
                if (packageInteractionInfo != null)
                    packageInteractionInfo.SetActive(false);
            }
        }

        if (other.CompareTag(boxTag) && other.TryGetComponent(out BoxDropZone box))
        {
            if (nearbyBox == box)
            {
                nearbyBox = null;
                if (leaveItem != null)
                    leaveItem.SetActive(false);
            }
        }

        if (other.CompareTag(npcTag))
        {
            isInNpcRange = false;
            if (conversationStarter != null)
                conversationStarter.SetActive(false);
        }
    }
}
