using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class NPCWander : MonoBehaviour
{
    [Header("Wander")]
    [Min(0.1f)] public float wanderRadius = 5f;
    [Min(0.1f)] public float walkSpeed = 1.2f;
    [Min(0f)] public float minimumPause = 1f;
    [Min(0f)] public float maximumPause = 3f;
    [Min(1f)] public float turnSpeed = 360f;
    [Min(0.05f)] public float arrivalDistance = 0.25f;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayers = ~0;
    [Min(0.05f)] public float obstacleCheckRadius = 0.22f;
    [Min(0.1f)] public float obstacleCheckDistance = 0.65f;

    [Header("Animation")]
    public Animator animator;
    public string movementParameter = "Blend";
    public float walkingAnimationValue = 1f;
    [Min(0f)] public float animationDampTime = 0.1f;

    Rigidbody body;
    Vector3 origin;
    Vector3 destination;
    float pauseTimer;
    int movementParameterHash;
    bool isWalking;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.constraints &= ~RigidbodyConstraints.FreezeRotationY;
        origin = transform.position;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        movementParameterHash = Animator.StringToHash(movementParameter);
        BeginPause();
    }

    void FixedUpdate()
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            SetWalking(false);

            if (pauseTimer <= 0f)
                ChooseDestination();

            return;
        }

        Vector3 toDestination = destination - body.position;
        toDestination.y = 0f;

        if (toDestination.sqrMagnitude <= arrivalDistance * arrivalDistance)
        {
            BeginPause();
            return;
        }

        Vector3 direction = toDestination.normalized;
        if (HasObstacleAhead(direction))
        {
            BeginPause(0.2f, 0.6f);
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        body.MoveRotation(Quaternion.RotateTowards(
            body.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime));

        Vector3 nextPosition =
            body.position + direction * walkSpeed * Time.fixedDeltaTime;
        nextPosition.y = body.position.y;
        body.MovePosition(nextPosition);
        SetWalking(true);
    }

    void OnDisable()
    {
        SetWalking(false);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isWalking)
            BeginPause(0.2f, 0.6f);
    }

    void ChooseDestination()
    {
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        destination = origin + new Vector3(offset.x, 0f, offset.y);

        Vector3 delta = destination - body.position;
        delta.y = 0f;
        if (delta.sqrMagnitude < 1f)
        {
            Vector2 fallback = Random.insideUnitCircle.normalized * wanderRadius;
            destination = origin + new Vector3(fallback.x, 0f, fallback.y);
        }
    }

    void BeginPause()
    {
        BeginPause(minimumPause, maximumPause);
    }

    void BeginPause(float minimum, float maximum)
    {
        pauseTimer = Random.Range(
            Mathf.Min(minimum, maximum),
            Mathf.Max(minimum, maximum));
        SetWalking(false);
    }

    bool HasObstacleAhead(Vector3 direction)
    {
        Vector3 originPoint = body.position + Vector3.up * 0.6f;
        RaycastHit[] hits = Physics.SphereCastAll(
            originPoint,
            obstacleCheckRadius,
            direction,
            obstacleCheckDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && hit.collider.transform.root != transform.root)
                return true;
        }

        return false;
    }

    void SetWalking(bool walking)
    {
        isWalking = walking;

        if (animator != null && !string.IsNullOrWhiteSpace(movementParameter))
        {
            animator.SetFloat(
                movementParameterHash,
                walking ? walkingAnimationValue : 0f,
                animationDampTime,
                Time.fixedDeltaTime);
        }
    }
}
