// ScreenBase.cs
using UnityEngine;

public abstract class ScreenBase : MonoBehaviour
{
    [field: SerializeField] public ScreenType ScreenType { get; private set; }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        OnShow();
    }

    public virtual void Hide()
    {
        OnHide();
        gameObject.SetActive(false);
    }

    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
}