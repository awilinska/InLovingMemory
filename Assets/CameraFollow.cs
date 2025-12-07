using UnityEngine;

public class CameraFollow : MonoBehaviour {
	
    public Transform target;
	public float smooth = 5f;
	public Vector3 offset;
	
	// Update is called once per frame
	void LateUpdate () {
		Vector3 position = target.position + offset;
		transform.position = Vector3.Lerp(transform.position, position, smooth * Time.deltaTime);
	}
}
