using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DisplayMessageTrigger : MonoBehaviour
{
    private TMP_Text MessageText;
    public string message;
    public float displayDuration = 3f; 

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        MessageText = GamePlayUIManager.Instance.MessageText;
        MessageText.text = message;
        MessageText.color = Color.red;
        MessageText.gameObject.SetActive(true);
        Invoke(nameof(DeactivateMessage), displayDuration); // Deactivate after -- seconds
    }

    void DeactivateMessage()
    {
        MessageText.gameObject.SetActive(false);
    }

}
