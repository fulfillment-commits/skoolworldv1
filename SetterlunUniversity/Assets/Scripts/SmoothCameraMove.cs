using UnityEngine;

public class SmoothCameraMove : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool shouldMove = false;

    void Update()
    {
        if (!shouldMove) return;

        // ✅ Move position
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        // ✅ Rotate to exact target rotation
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * 100f * Time.deltaTime
        );

        // ✅ Stop when both reached
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f &&
            Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            shouldMove = false;
        }
    }

    public void StopMovingCamera()
    {     
        shouldMove = false;
    }

    public void MoveCamera(Transform target)
    {
        targetPosition = target.position;
        targetRotation = target.rotation;
        shouldMove = true;
    }
}
