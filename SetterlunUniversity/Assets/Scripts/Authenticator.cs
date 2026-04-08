using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Authenticator : MonoBehaviour
{
    [Header("Objects To Toggle")]
    public GameObject[] DeactivatingObjects;
    public GameObject[] ActivatingObjects;

    [Header("UI")]
    private Button AuthenticatingButton;
    private Slider holdSlider;
    public float holdDuration = 2f;

    [Header("Facing Settings")]
    public float rotationSpeed = 6f;

    private bool isHolding = false;
    private bool shouldFaceTarget = false;
    private float holdTimer = 0f;

    private GameObject player;

    private TMP_Text messageText;

    private void OnEnable()
    {
        AuthenticatingButton = GamePlayUIManager.Instance.AuthenticateButton;
        holdSlider = GamePlayUIManager.Instance.AuthenticateSlider;
        messageText = GamePlayUIManager.Instance.MessageText;  
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        player = other.gameObject;

        SetupHoldEvents();

        AuthenticatingButton.gameObject.SetActive(true);
        holdSlider.gameObject.SetActive(true);
        holdSlider.value = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        ResetAuthentication();
        AuthenticatingButton.gameObject.SetActive(false);
        holdSlider.gameObject.SetActive(false);
    }

    void SetupHoldEvents()
    {
        EventTrigger trigger = AuthenticatingButton.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = AuthenticatingButton.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        // Pointer Down
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { StartHolding(); });
        trigger.triggers.Add(entryDown);

        // Pointer Up
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => { StopHolding(); });
        trigger.triggers.Add(entryUp);
    }

    void Update()
    {
        HandleHolding();
        HandleFacing();
    }

    void HandleHolding()
    {
        if (!isHolding) return;

        holdTimer += Time.deltaTime;
        holdSlider.value = holdTimer / holdDuration;

        if (holdTimer >= holdDuration)
        {
            holdSlider.value = 1f;
            isHolding = false;
            shouldFaceTarget = false;

            AuthenticationPassed();
        }
    }

    void HandleFacing()
    {
        if (!shouldFaceTarget || player == null) return;

        Vector3 direction = transform.position - player.transform.position;
        direction.y = 0f; // Prevent vertical tilt

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        player.transform.rotation = Quaternion.Slerp(
            player.transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    void StartHolding()
    {
        if (player == null) return;

        isHolding = true;
        shouldFaceTarget = true;
        holdTimer = 0f;

        Animator anim = player.GetComponent<Animator>();
        if (anim != null)
        {
            anim.Play("Authenticating");
            anim.SetBool("Authenticate", true);
        }
    }

    void StopHolding()
    {
        ResetAuthentication();
    }

    void ResetAuthentication()
    {
        isHolding = false;
        shouldFaceTarget = false;
        holdTimer = 0f;

        Animator anim = player.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("Authenticate", false);
        }

        if (holdSlider != null)
            holdSlider.value = 0f;
    }

    void AuthenticationPassed()
    {
        foreach (GameObject obj in DeactivatingObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        foreach (GameObject obj in ActivatingObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        AuthenticatingButton.gameObject.SetActive(false);
        holdSlider.gameObject.SetActive(false);

        gameObject.SetActive(false);
        messageText.text = "Authentication Successful!";
        messageText.color = Color.green;
        messageText.gameObject.SetActive(true);
        Invoke(nameof(DeactivateMessage), 3f); // Deactivate after 3 seconds

        Animator anim = player.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("Authenticate", false);
        }
    }


    void DeactivateMessage()
    {
        messageText.gameObject.SetActive(false);
    }
}
