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
        TalkToGrandmaAfterTwoPackages,
        TellGrandma,
        Complete
    }

    [Header("References")]
    public PlayerPackageInteractor playerInteractor;
    public TMP_Text objectiveText;
    public GameObject newTaskObject;
    public WideSplitCameraComposite splitCamera;

    const float MinimumCameraSlice = 0.001f;

    [Header("New Task Notification")]
    public float newTaskDisplaySeconds = 5f;

    [Header("Objectives")]
    public string objectiveHeader = "TO DO:";
    public string talkToGrandmaTask = "Talk to grandma";
    public string packBoxesTask = "Pack the rest of the boxes";
    public string tellGrandmaTask = "Tell grandma that you're done";
    public string completedTask = "All done";

    [Header("Grandma Interrupt")]
    [Min(1)]
    public int packagesBeforeGrandmaInterrupt = 2;
    [Range(MinimumCameraSlice, 1f - MinimumCameraSlice)]
    public float startingLeftCameraSeam = 0.999f;
    [Range(MinimumCameraSlice, 1f - MinimumCameraSlice)]
    public float grandmaPeekSeam = 0.75f;
    [Range(MinimumCameraSlice, 1f - MinimumCameraSlice)]
    public float grandmaFullRightSeam = 0.001f;
    public float grandmaPeekSeconds = 3f;
    public float grandmaFullRightSeconds = 2f;

    [Header("Story Dialogue")]
    [TextArea(2, 5)]
    public string packingDialogue = "We should pack the things into the boxes.";

    [TextArea(2, 5)]
    public string grandmaInterruptDialogue = "GRANDMA: Could you come here for a moment?";

    [TextArea(2, 5)]
    public string grandmaCheckDialogueFirst = "GRANDMA: I thought I saw something over here.";

    [TextArea(2, 5)]
    public string grandmaCheckDialogueSecond = "GRANDMA: Never mind, dear. Let's keep going.";

    [TextArea(2, 5)]
    public string completionDialogue = "Great job, we did it in no time!";

    FlowStage stage;
    Coroutine newTaskRoutine;
    Coroutine cameraSeamRoutine;
    Coroutine grandmaCheckDialogueRoutine;
    bool grandmaInterruptStarted;

    void Awake()
    {
        if (playerInteractor == null)
            playerInteractor = GetComponent<PlayerPackageInteractor>();

        if (playerInteractor != null)
            playerInteractor.SetAlternatePackageNpcDialogueEnabled(false);

        if (splitCamera == null)
            splitCamera = FindAnyObjectByType<WideSplitCameraComposite>();

        SetCameraSeam(startingLeftCameraSeam);

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

        if (stage == FlowStage.TalkToGrandmaAfterTwoPackages)
        {
            if (grandmaCheckDialogueRoutine == null)
                grandmaCheckDialogueRoutine = StartCoroutine(ShowGrandmaCheckDialogues());

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
        {
            SetPackingObjective(delivered, total);

            if (!grandmaInterruptStarted && delivered >= packagesBeforeGrandmaInterrupt)
                StartGrandmaInterrupt(delivered, total);
        }
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
            case FlowStage.TalkToGrandmaAfterTwoPackages:
                SetPausedPackingObjective(
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

    void SetPausedPackingObjective(int delivered, int total)
    {
        SetObjective($"<s>{packBoxesTask} ({delivered}/{total})</s>\n{talkToGrandmaTask}");
    }

    void StartGrandmaInterrupt(int delivered, int total)
    {
        grandmaInterruptStarted = true;
        SetStage(FlowStage.TalkToGrandmaAfterTwoPackages);
        SetPausedPackingObjective(delivered, total);
        playerInteractor.ShowDialogueMessage(grandmaInterruptDialogue);
        StartCameraSeamTransition(grandmaPeekSeam, grandmaPeekSeconds);
    }

    IEnumerator ShowGrandmaCheckDialogues()
    {
        StartCameraSeamTransition(grandmaFullRightSeam, grandmaFullRightSeconds);
        playerInteractor.ShowDialogueMessage(grandmaCheckDialogueFirst);
        yield return WaitForDialoguePanelToClose();

        playerInteractor.ShowDialogueMessage(grandmaCheckDialogueSecond);
        yield return WaitForDialoguePanelToClose();

        grandmaCheckDialogueRoutine = null;
        playerInteractor.SetAlternatePackageNpcDialogueEnabled(true);
        ResumeMainQuestAfterGrandmaInterrupt();
    }

    IEnumerator WaitForDialoguePanelToClose()
    {
        yield return null;

        if (playerInteractor.dialoguePanel == null)
            yield break;

        while (playerInteractor.dialoguePanel.activeSelf)
            yield return null;
    }

    void ResumeMainQuestAfterGrandmaInterrupt()
    {
        if (playerInteractor.TotalPackageCount > 0
            && playerInteractor.DeliveredPackageCount >= playerInteractor.TotalPackageCount)
        {
            SetStage(FlowStage.TellGrandma);
            return;
        }

        SetStage(FlowStage.PackBoxes);
    }

    void StartCameraSeamTransition(float targetSeam, float duration)
    {
        if (cameraSeamRoutine != null)
            StopCoroutine(cameraSeamRoutine);

        cameraSeamRoutine = StartCoroutine(AnimateCameraSeam(targetSeam, duration));
    }

    IEnumerator AnimateCameraSeam(float targetSeam, float duration)
    {
        if (splitCamera == null)
        {
            cameraSeamRoutine = null;
            yield break;
        }

        float startSeam = splitCamera.seam;
        float clampedTarget = ClampSafeSeam(targetSeam);
        float elapsed = 0f;

        if (duration <= 0f)
        {
            SetCameraSeam(clampedTarget);
            cameraSeamRoutine = null;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetCameraSeam(Mathf.SmoothStep(startSeam, clampedTarget, t));
            yield return null;
        }

        SetCameraSeam(clampedTarget);
        cameraSeamRoutine = null;
    }

    void SetCameraSeam(float seam)
    {
        if (splitCamera != null)
        {
            splitCamera.seam = ClampSafeSeam(seam);
            splitCamera.Apply();
        }
    }

    static float ClampSafeSeam(float seam)
    {
        return Mathf.Clamp(seam, MinimumCameraSlice, 1f - MinimumCameraSlice);
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
