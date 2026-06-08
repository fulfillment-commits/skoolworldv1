using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DeactivateObject : MonoBehaviour
{
    public float timeToDeactivateAfter = 2f;
    public UnityEvent onDeactivate;
    private void OnEnable()
    {
        Invoke(nameof(DeactivateThisObject), timeToDeactivateAfter);
    }

    private void DeactivateThisObject()
    {
        onDeactivate?.Invoke();
        gameObject.SetActive(false);
    }
}
