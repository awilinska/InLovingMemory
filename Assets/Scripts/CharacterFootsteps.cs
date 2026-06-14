using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
[DefaultExecutionOrder(100)]
public sealed class CharacterFootsteps : MonoBehaviour
{
    [Header("Walking")]
    [SerializeField] float minimumWalkingSpeed = 0.15f;
    [SerializeField] float strideDistance = 0.72f;

    [Header("Sound")]
    [SerializeField] AudioClip[] footstepClips;
    [SerializeField] float volume = 0.38f;
    [SerializeField] float minimumPitch = 0.93f;
    [SerializeField] float maximumPitch = 1.07f;

    AudioSource audioSource;
    Vector3 previousPosition;
    float distanceSinceStep;
    bool wasWalking;
    int nextStep;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.dopplerLevel = 0f;
        audioSource.minDistance = 1.5f;
        audioSource.maxDistance = 14f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        previousPosition = transform.position;
    }

    void Update()
    {
        Vector3 currentPosition = transform.position;
        Vector3 movement = currentPosition - previousPosition;
        previousPosition = currentPosition;
        movement.y = 0f;

        float distance = movement.magnitude;
        float speed = Time.deltaTime > 0f ? distance / Time.deltaTime : 0f;
        bool isWalking = speed >= minimumWalkingSpeed && distance < 1f;

        if (!isWalking)
        {
            wasWalking = false;
            distanceSinceStep = 0f;
            return;
        }

        if (!wasWalking)
            distanceSinceStep = strideDistance * 0.55f;

        wasWalking = true;
        distanceSinceStep += distance;

        if (distanceSinceStep < strideDistance)
            return;

        distanceSinceStep %= strideDistance;
        PlayStep();
    }

    void PlayStep()
    {
        AudioClip clip = GetNextClip();
        if (clip == null)
            return;

        audioSource.pitch = Random.Range(minimumPitch, maximumPitch);
        audioSource.PlayOneShot(clip, volume * Random.Range(0.9f, 1.05f));
    }

    AudioClip GetNextClip()
    {
        if (footstepClips == null || footstepClips.Length == 0)
            return null;

        for (int i = 0; i < footstepClips.Length; i++)
        {
            AudioClip clip = footstepClips[nextStep];
            nextStep = (nextStep + 1) % footstepClips.Length;

            if (clip != null)
                return clip;
        }

        return null;
    }
}
