using DG.Tweening;
using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [SerializeField] private Transform camTransform;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeStrength = 0.4f;

    private Tween currentShake;

    public void Shake()
    {
        // Kill any running shake first
        currentShake?.Kill();

        // "DOShakePosition" = random offset-based shake animation
        currentShake = camTransform.DOShakePosition(
            duration: shakeDuration,
            strength: shakeStrength,
            vibrato: 10,
            randomness: 90,
            fadeOut: true
        );
    }

    [Header("Zoom Settings")]
    [SerializeField] private float zoomDistance = 2.5f;    // How far in front of the target
    [SerializeField] private float zoomHeight = 1.5f;      // Camera height offset (eye level)
    [SerializeField] private float lookAtHeight = 1.2f;    // Where on the target to look (upper body/face)

    /// <summary>
    /// Smoothly moves the camera to frame a target (e.g., the winner during victory sequence).
    /// </summary>
    public void ZoomToTarget(Transform target, float duration)
    {
        if (target == null) return;

        // Kill any running shake first
        currentShake?.Kill();

        // Calculate a position in front of the target, offset by zoomDistance
        Vector3 targetForward = target.forward;
        Vector3 cameraPosition = target.position 
            + targetForward * zoomDistance    // In front of the target
            + Vector3.up * zoomHeight;        // At eye level

        // The point to look at (upper body/face height)
        Vector3 lookAtPoint = target.position + Vector3.up * lookAtHeight;

        // Calculate the final rotation the camera should have when it arrives
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - cameraPosition);

        // Smoothly move camera to the offset position
        camTransform.DOMove(cameraPosition, duration).SetEase(Ease.InOutQuad);
        
        // Smoothly rotate to face the target (using the pre-calculated final rotation)
        camTransform.DORotateQuaternion(targetRotation, duration).SetEase(Ease.InOutQuad);
    }
}
