using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("UI/On Enable Assign Component")]
public class OnEnableAssignComponent : MonoBehaviour
{
    [Header("Target")]
    public RectTransform form;

    [Header("Size")]
    public Vector2 setForm = new Vector2(624f, 635f);

    [Header("Apply Settings")]
    [SerializeField] private bool callInEditMode = true;
    [SerializeField] private bool callInPlayMode = true;
    [SerializeField] private bool applyWhenValuesChangeInInspector = true;
    [SerializeField] private bool onlyApplyWhenObjectIsActive = true;

    [Header("Layout Settings")]
    [SerializeField] private bool setLayoutElementPreferredSize = false;
    [SerializeField] private bool forceRebuildLayout = true;

    [Header("Safety")]
    [SerializeField] private bool preventRecursiveCalls = true;
    [SerializeField] private bool logResult = false;

    private bool isApplying;

    private void Reset()
    {
        form = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        ApplySize();
    }

    private void OnValidate()
    {
        setForm.x = Mathf.Max(0f, setForm.x);
        setForm.y = Mathf.Max(0f, setForm.y);

        if (!applyWhenValuesChangeInInspector)
            return;

        ApplySize();
    }

    public void SetForm(Vector2 newSize)
    {
        setForm = new Vector2(
            Mathf.Max(0f, newSize.x),
            Mathf.Max(0f, newSize.y)
        );

        ApplySize();
    }

    public void SetWidth(float width)
    {
        setForm.x = Mathf.Max(0f, width);
        ApplySize();
    }

    public void SetHeight(float height)
    {
        setForm.y = Mathf.Max(0f, height);
        ApplySize();
    }

    public void ApplySize()
    {
        ApplySizeInternal(false);
    }

    public void ApplySizeNow()
    {
        ApplySizeInternal(true);
    }

    private void ApplySizeInternal(bool forceApply)
    {
        if (form == null)
            return;

        if (preventRecursiveCalls && isApplying)
            return;

        if (!forceApply && !CanApplyNow())
            return;

        isApplying = true;

        float targetWidth = Mathf.Max(0f, setForm.x);
        float targetHeight = Mathf.Max(0f, setForm.y);

        form.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        form.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

        if (setLayoutElementPreferredSize)
        {
            LayoutElement layoutElement = form.GetComponent<LayoutElement>();

            if (layoutElement != null)
            {
                layoutElement.preferredWidth = targetWidth;
                layoutElement.preferredHeight = targetHeight;
            }
        }

        if (forceRebuildLayout)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(form);
            Canvas.ForceUpdateCanvases();
        }

#if UNITY_EDITOR
        MarkDirtyInEditor();
#endif

        if (logResult)
        {
            Debug.Log(
                "[OnEnableAssignComponent] Applied size " +
                targetWidth.ToString("0.##") +
                " x " +
                targetHeight.ToString("0.##") +
                " to " +
                form.name,
                this
            );
        }

        isApplying = false;
    }

    private bool CanApplyNow()
    {
        bool isPlaying = Application.isPlaying;

        if (isPlaying && !callInPlayMode)
            return false;

        if (!isPlaying && !callInEditMode)
            return false;

        if (onlyApplyWhenObjectIsActive && !gameObject.activeInHierarchy)
            return false;

        return true;
    }

#if UNITY_EDITOR
    private void MarkDirtyInEditor()
    {
        if (Application.isPlaying)
            return;

        if (form == null)
            return;

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.EditorUtility.SetDirty(form);

        if (form.gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(form.gameObject.scene);
        }
    }
#endif
}