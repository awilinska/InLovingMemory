using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(PlayerPackageInteractor))]
public class PrototypeFlowController : MonoBehaviour
{
    enum FlowStage
    {
        TalkToGrandma,
        PackBoxes,
        TellGrandma,
        Complete
    }

    [Header("References")]
    public PlayerPackageInteractor playerInteractor;
    public TMP_Text objectiveText;
    public GameObject newTaskObject;

    [Header("New Task Notification")]
    public float newTaskDisplaySeconds = 5f;

    [Header("Objectives")]
    public string objectiveHeader = "TO DO:";
    public string talkToGrandmaTask = "Talk to grandma";
    public string packBoxesTask = "Pack the rest of the boxes";
    public string tellGrandmaTask = "Tell grandma that you're done";
    public string completedTask = "All done";

    [Header("Story Dialogue")]
    [TextArea(2, 5)]
    public string packingDialogue = "We should pack the things into the boxes.";

    [TextArea(2, 5)]
    public string completionDialogue = "Great job, we did it in no time!";

    FlowStage stage;
    Coroutine newTaskRoutine;

    void Awake()
    {
        if (playerInteractor == null)
            playerInteractor = GetComponent<PlayerPackageInteractor>();

        if (newTaskObject != null)
            newTaskObject.SetActive(false);
    }

    void OnEnable()
    {
        if (playerInteractor == null)
            return;

        playerInteractor.NpcTalkRequested += HandleNpcTalk;
        playerInteractor.PackageProgressChanged += HandlePackageProgressChanged;
        playerInteractor.PackagesCompleted += HandlePackagesCompleted;
    }

    void Start()
    {
        SetStage(FlowStage.TalkToGrandma);
    }

    void OnDisable()
    {
        HideNewTaskNotification();

        if (playerInteractor == null)
            return;

        playerInteractor.NpcTalkRequested -= HandleNpcTalk;
        playerInteractor.PackageProgressChanged -= HandlePackageProgressChanged;
        playerInteractor.PackagesCompleted -= HandlePackagesCompleted;
    }

    bool HandleNpcTalk()
    {
        if (stage == FlowStage.TalkToGrandma)
        {
            playerInteractor.ShowDialogueMessage(packingDialogue);
            SetStage(FlowStage.PackBoxes);
            return true;
        }

        if (stage == FlowStage.TellGrandma)
        {
            playerInteractor.ShowDialogueMessage(completionDialogue);
            SetStage(FlowStage.Complete);
            return true;
        }

        return false;
    }

    void HandlePackagesCompleted()
    {
        if (stage == FlowStage.PackBoxes)
            SetStage(FlowStage.TellGrandma);
    }

    void HandlePackageProgressChanged(int delivered, int total)
    {
        if (stage == FlowStage.PackBoxes)
            SetPackingObjective(delivered, total);
    }

    void SetStage(FlowStage newStage)
    {
        stage = newStage;
        playerInteractor.SetPackageInteractionEnabled(stage == FlowStage.PackBoxes);

        switch (stage)
        {
            case FlowStage.TalkToGrandma:
                SetObjective(talkToGrandmaTask);
                break;
            case FlowStage.PackBoxes:
                SetPackingObjective(
                    playerInteractor.DeliveredPackageCount,
                    playerInteractor.TotalPackageCount);
                break;
            case FlowStage.TellGrandma:
                SetObjective(tellGrandmaTask);
                break;
            case FlowStage.Complete:
                SetObjective(completedTask);
                break;
        }

        if (stage == FlowStage.Complete)
            HideNewTaskNotification();
        else
            ShowNewTaskNotification();
    }

    void SetObjective(string task)
    {
        if (objectiveText != null)
            objectiveText.text = $"{objectiveHeader}\n{task}";
    }

    void SetPackingObjective(int delivered, int total)
    {
        SetObjective($"{packBoxesTask} ({delivered}/{total})");
    }

    void ShowNewTaskNotification()
    {
        if (newTaskObject == null)
            return;

        if (newTaskRoutine != null)
            StopCoroutine(newTaskRoutine);

        newTaskObject.SetActive(true);
        newTaskRoutine = StartCoroutine(HideNewTaskNotificationAfterDelay());
    }

    IEnumerator HideNewTaskNotificationAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, newTaskDisplaySeconds));

        newTaskObject.SetActive(false);
        newTaskRoutine = null;
    }

    void HideNewTaskNotification()
    {
        if (newTaskRoutine != null)
        {
            StopCoroutine(newTaskRoutine);
            newTaskRoutine = null;
        }

        if (newTaskObject != null)
            newTaskObject.SetActive(false);
    }
}
