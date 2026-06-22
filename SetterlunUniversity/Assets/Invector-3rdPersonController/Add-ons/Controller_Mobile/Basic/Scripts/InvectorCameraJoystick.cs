using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityStandardAssets.CrossPlatformInput;

public class InvectorCameraJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public enum AxisOption
    {
        Both,
        OnlyHorizontal,
        OnlyVertical
    }

    [Header("Joystick")]
    public int movementRange = 100;
    public AxisOption axesToUse = AxisOption.Both;

    [Header("Camera Axis Names")]
    public string horizontalAxisName = "Mouse X";
    public string verticalAxisName = "Mouse Y";

    [Header("Sensitivity")]
    public float sensitivityX = 2f;
    public float sensitivityY = 2f;
    [Range(0f, 0.5f)] public float deadZone = 0.08f;
    public float smoothSpeed = 14f;
    public bool invertX;
    public bool invertY;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Canvas parentCanvas;
    private Vector3 startLocalPosition;
    private Vector2 currentAxis;
    private Vector2 smoothedAxis;
    private bool isPressed;
    private bool useX;
    private bool useY;
    private CrossPlatformInputManager.VirtualAxis horizontalAxis;
    private CrossPlatformInputManager.VirtualAxis verticalAxis;

    private IEnumerator Start()
    {
        rectTransform = transform as RectTransform;
        parentRect = transform.parent as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();

        if (rectTransform != null)
        {
            startLocalPosition = rectTransform.localPosition;
        }

        yield return new WaitForEndOfFrame();
        CreateVirtualAxes();
    }

    private void Update()
    {
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
        smoothedAxis = Vector2.Lerp(smoothedAxis, currentAxis, t);

        if (!isPressed && smoothedAxis.sqrMagnitude < 0.0001f)
        {
            smoothedAxis = Vector2.zero;
        }

        UpdateVirtualAxes(smoothedAxis);
    }

    private void CreateVirtualAxes()
    {
        useX = axesToUse == AxisOption.Both || axesToUse == AxisOption.OnlyHorizontal;
        useY = axesToUse == AxisOption.Both || axesToUse == AxisOption.OnlyVertical;

        if (useX)
        {
            horizontalAxis = CrossPlatformInputManager.AxisExists(horizontalAxisName)
                ? CrossPlatformInputManager.VirtualAxisReference(horizontalAxisName)
                : new CrossPlatformInputManager.VirtualAxis(horizontalAxisName);

            if (!CrossPlatformInputManager.AxisExists(horizontalAxisName))
            {
                CrossPlatformInputManager.RegisterVirtualAxis(horizontalAxis);
            }
        }

        if (useY)
        {
            verticalAxis = CrossPlatformInputManager.AxisExists(verticalAxisName)
                ? CrossPlatformInputManager.VirtualAxisReference(verticalAxisName)
                : new CrossPlatformInputManager.VirtualAxis(verticalAxisName);

            if (!CrossPlatformInputManager.AxisExists(verticalAxisName))
            {
                CrossPlatformInputManager.RegisterVirtualAxis(verticalAxis);
            }
        }
    }

    public void OnDrag(PointerEventData data)
    {
        if (rectTransform == null || parentRect == null)
        {
            return;
        }

        Camera eventCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, data.position, eventCamera, out Vector2 localPoint))
        {
            return;
        }

        Vector2 delta = Vector2.ClampMagnitude(localPoint - (Vector2)startLocalPosition, movementRange);
        rectTransform.localPosition = startLocalPosition + new Vector3(delta.x, delta.y, 0f);

        currentAxis = movementRange > 0 ? ApplyDeadZone(delta / movementRange) : Vector2.zero;
    }

    public void OnPointerDown(PointerEventData data)
    {
        isPressed = true;
        OnDrag(data);
    }

    public void OnPointerUp(PointerEventData data)
    {
        if (rectTransform != null)
        {
            rectTransform.localPosition = startLocalPosition;
        }

        isPressed = false;
        currentAxis = Vector2.zero;
    }

    private void OnDisable()
    {
        currentAxis = Vector2.zero;
        smoothedAxis = Vector2.zero;
        UpdateVirtualAxes(Vector2.zero);
    }

    private void UpdateVirtualAxes(Vector2 axis)
    {
        if (useX && horizontalAxis != null)
        {
            horizontalAxis.Update((invertX ? -axis.x : axis.x) * sensitivityX);
        }

        if (useY && verticalAxis != null)
        {
            verticalAxis.Update((invertY ? -axis.y : axis.y) * sensitivityY);
        }
    }

    private Vector2 ApplyDeadZone(Vector2 axis)
    {
        float magnitude = axis.magnitude;
        if (magnitude <= deadZone)
        {
            return Vector2.zero;
        }

        float scaledMagnitude = Mathf.InverseLerp(deadZone, 1f, magnitude);
        return axis.normalized * scaledMagnitude;
    }
}
