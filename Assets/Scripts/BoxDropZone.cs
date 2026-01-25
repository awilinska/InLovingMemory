using UnityEngine;

public class BoxDropZone : MonoBehaviour
{
    [Tooltip("Optional point where packages should land when dropped into this box.")]
    public Transform dropPoint;

    public Transform GetDropPoint()
    {
        return dropPoint != null ? dropPoint : transform;
    }
}
