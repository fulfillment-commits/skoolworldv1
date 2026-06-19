using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ASAD_Multiplyer.Network;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ASAD_Multiplyer.Chat
{
    public class PUN_ChatManager : MonoBehaviourPunCallbacks
    {
        private const string PlayerPrefsUserId = "OnboardingUserId_Str";
        private const string PlayerPrefsUsername = "OnboardingUsername";

        public static PUN_ChatManager Instance { get; private set; }

        [Header("Runtime")]
        [SerializeField] private bool autoBuildUiIfMissing = true;
        [SerializeField] private float activeChatRefreshSeconds = 3f;
        [SerializeField] private float unreadRefreshSeconds = 5f;
        [SerializeField] private int messageLoadLimit = FirestoreChatService.DefaultMessageLimit;
        [SerializeField, HideInInspector] private int generatedUiVersion;

        [Header("Root")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField] private Button chatToggleButton;
        [SerializeField] private TextMeshProUGUI toggleUnreadText;

        [Header("User List")]
        [SerializeField] private GameObject userListPanel;
        [SerializeField] private Transform userListContent;
        [SerializeField] private TextMeshProUGUI userListStatusText;
        [SerializeField] private PUN_ChatListItem userListItemPrefab;
        [SerializeField] private Button userChatTabButton;
        [SerializeField] private Button publicChatTabButton;

        [Header("Chat Panel")]
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private TextMeshProUGUI chatHeaderText;
        [SerializeField] private TextMeshProUGUI chatHeaderAvatarText;
        [SerializeField] private TextMeshProUGUI chatStatusText;
        [SerializeField] private ScrollRect messagesScrollRect;
        [SerializeField] private Transform messagesContent;
        [SerializeField] private TMP_InputField messageInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button closeChatButton;
        [SerializeField] private PUN_ChatMessageItem outgoingMessagePrefab;
        [SerializeField] private PUN_ChatMessageItem incomingMessagePrefab;

        private readonly Dictionary<int, PUN_ChatListItem> visiblePlayers = new Dictionary<int, PUN_ChatListItem>();
        private readonly HashSet<string> unreadUserIds = new HashSet<string>();
        private readonly HashSet<string> unreadRequestsInFlight = new HashSet<string>();
        private readonly HashSet<string> renderedMessageIds = new HashSet<string>();
        private enum ChatMode { Private, Public }

        public bool showChatBtn=true;
        private ChatMode activeChatMode = ChatMode.Private;
        private Player activePlayer;
        private string activeUserId = "";
        private string activeDisplayName = "";
        private Coroutine refreshRoutine;
        private float nextUserListRefreshTime;
        private float nextUnreadRefreshTime;
        private bool isLoadingMessages;
        private bool isSendingMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance != null || FindObjectOfType<PUN_ChatManager>() != null)
            {
                return;
            }

            GameObject managerObject = new GameObject("PUN_ChatManager");
            managerObject.AddComponent<PUN_ChatManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoBuildUiIfMissing && canvas == null)
            {
                PUN_ChatRuntimeUiBuilder.Build(this);
            }

            BindButtons();
            EnsurePublicChatUiExtensions();
            SetJoinedUiVisible(false);
            SetUserListVisible(false);
            SetChatVisible(false);
        }

        private void Start()
        {
            SyncLocalPhotonIdentityFromOnboarding();
            RefreshVisibility();
            RefreshUserList();
        }

        private void Update()
        {
            RefreshVisibility();
            SyncLocalPhotonIdentityFromOnboarding();
            if (IsLocalPlayerReady() && Time.unscaledTime >= nextUserListRefreshTime)
            {
                nextUserListRefreshTime = Time.unscaledTime + 2f;
                RefreshUserList();
            }

            if (IsLocalPlayerReady() && Time.unscaledTime >= nextUnreadRefreshTime)
            {
                nextUnreadRefreshTime = Time.unscaledTime + Mathf.Max(2f, unreadRefreshSeconds);
                RefreshUnreadIndicators();
            }

            if (messageInput != null && messageInput.isFocused && Input.GetKeyDown(KeyCode.Return))
            {
                SendCurrentMessage();
            }
        }

        public void AssignReferences(
            Canvas assignedCanvas,
            CanvasGroup assignedRootCanvasGroup,
            Button assignedChatToggleButton,
            TextMeshProUGUI assignedToggleUnreadText,
            GameObject assignedUserListPanel,
            Transform assignedUserListContent,
            TextMeshProUGUI assignedUserListStatusText,
            PUN_ChatListItem assignedUserListItemPrefab,
            GameObject assignedChatPanel,
            TextMeshProUGUI assignedChatHeaderText,
            TextMeshProUGUI assignedChatHeaderAvatarText,
            TextMeshProUGUI assignedChatStatusText,
            ScrollRect assignedMessagesScrollRect,
            Transform assignedMessagesContent,
            TMP_InputField assignedMessageInput,
            Button assignedSendButton,
            Button assignedCloseChatButton,
            PUN_ChatMessageItem assignedOutgoingMessagePrefab,
            PUN_ChatMessageItem assignedIncomingMessagePrefab)
        {
            canvas = assignedCanvas;
            rootCanvasGroup = assignedRootCanvasGroup;
            chatToggleButton = assignedChatToggleButton;
            toggleUnreadText = assignedToggleUnreadText;
            userListPanel = assignedUserListPanel;
            userListContent = assignedUserListContent;
            userListStatusText = assignedUserListStatusText;
            userListItemPrefab = assignedUserListItemPrefab;
            chatPanel = assignedChatPanel;
            chatHeaderText = assignedChatHeaderText;
            chatHeaderAvatarText = assignedChatHeaderAvatarText;
            chatStatusText = assignedChatStatusText;
            messagesScrollRect = assignedMessagesScrollRect;
            messagesContent = assignedMessagesContent;
            messageInput = assignedMessageInput;
            sendButton = assignedSendButton;
            closeChatButton = assignedCloseChatButton;
            outgoingMessagePrefab = assignedOutgoingMessagePrefab;
            incomingMessagePrefab = assignedIncomingMessagePrefab;

            BindButtons();
        }

        internal void SetGeneratedUiVersion(int version)
        {
            generatedUiVersion = version;
        }

        public void ToggleUserList()
        {
            if (!IsLocalPlayerReady())
            {
                return;
            }

            bool show = userListPanel == null || !userListPanel.activeSelf;
            if (show)
            {
                StopRefreshRoutine();
                SetChatVisible(false);
                activeChatMode = ChatMode.Private;
                activePlayer = null;
                activeUserId = "";
                activeDisplayName = "";
            }

            SetUserListVisible(show);
            if (show)
            {
                SetActiveTab(ChatMode.Private);
                RefreshUserList();
            }
        }

        public void OpenPublicChat()
        {
            if (!IsLocalPlayerReady())
            {
                return;
            }

            activeChatMode = ChatMode.Public;
            activePlayer = null;
            activeDisplayName = "All Chat";
            activeUserId = FirestoreChatService.PublicChatId;
            renderedMessageIds.Clear();
            ClearMessages();

            SetActiveTab(ChatMode.Public);
            SetChatVisible(true);
            SetUserListVisible(false);

            if (chatHeaderText != null)
            {
                chatHeaderText.text = "All Chat";
            }

            if (chatHeaderAvatarText != null)
            {
                chatHeaderAvatarText.text = "#";
            }

            SetStatus("Loading public messages...");
            LoadActiveMessages(true);
            RestartRefreshRoutine();
        }

        public void OpenChat(PUN_ChatListItem item)
        {
            if (item == null)
            {
                return;
            }

            OpenChat(item.PhotonPlayer, item.DisplayName, item.UserId);
        }

        public void OpenChat(Player player, string displayName, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                SetStatus("Missing user id for selected player.");
                return;
            }

            activeChatMode = ChatMode.Private;
            activePlayer = player;
            activeDisplayName = string.IsNullOrWhiteSpace(displayName) ? "User" : displayName.Trim();
            activeUserId = userId.Trim();
            unreadUserIds.Remove(activeUserId);
            UpdateUnreadIndicators();
            renderedMessageIds.Clear();
            ClearMessages();

            SetChatVisible(true);
            SetUserListVisible(false);

            if (chatHeaderText != null)
            {
                chatHeaderText.text = activeDisplayName;
            }

            if (chatHeaderAvatarText != null)
            {
                chatHeaderAvatarText.text = GetInitials(activeDisplayName);
            }

            SetStatus("Loading messages...");
            LoadActiveMessages(true);
            RestartRefreshRoutine();
        }

        public void CloseChat()
        {
            activePlayer = null;
            activeUserId = "";
            activeDisplayName = "";
            StopRefreshRoutine();
            SetChatVisible(false);
            if (IsLocalPlayerReady())
            {
                SetUserListVisible(true);
                SetActiveTab(ChatMode.Private);
                RefreshUserList();
            }
        }

        public void SendCurrentMessage()
        {
            if (isSendingMessage || messageInput == null)
            {
                return;
            }

            string text = messageInput.text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (!TryGetLocalIdentity(out string myDisplayName, out string myUserId))
            {
                SetStatus("Login is required before chat can send.");
                return;
            }

            if (activeChatMode == ChatMode.Private && string.IsNullOrEmpty(activeUserId))
            {
                SetStatus("Select a user first.");
                return;
            }

            isSendingMessage = true;
            SetSendInteractable(false);
            SetStatus("Sending...");

            string messageText = text.Trim();
            messageInput.text = "";

            if (activeChatMode == ChatMode.Public)
            {
                FirestoreChatService.SendPublicMessage(
                    myUserId,
                    myDisplayName,
                    messageText,
                    _ => HandleMessageSent(),
                    error => HandleMessageSendFailed(error, messageText));
            }
            else
            {
                FirestoreChatService.SendMessage(
                    myUserId,
                    activeUserId,
                    messageText,
                    _ => HandleMessageSent(),
                    error => HandleMessageSendFailed(error, messageText),
                    myDisplayName,
                    activeDisplayName);
            }
        }

        public override void OnJoinedRoom()
        {
            RefreshVisibility();
            RefreshUserList();
        }

        public override void OnLeftRoom()
        {
            visiblePlayers.Clear();
            unreadUserIds.Clear();
            unreadRequestsInFlight.Clear();
            CloseChat();
            UpdateUnreadIndicators();
            RefreshVisibility();
            RefreshUserList();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            RefreshUserList();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (otherPlayer != null && activePlayer != null && otherPlayer.ActorNumber == activePlayer.ActorNumber)
            {
                CloseChat();
            }

            RefreshUserList();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            RefreshUserList();
        }

        private void BindButtons()
        {
            if (chatToggleButton != null)
            {
                chatToggleButton.onClick.RemoveListener(ToggleUserList);
                chatToggleButton.onClick.AddListener(ToggleUserList);
            }

            if (sendButton != null)
            {
                sendButton.onClick.RemoveListener(SendCurrentMessage);
                sendButton.onClick.AddListener(SendCurrentMessage);
            }

            if (closeChatButton != null)
            {
                closeChatButton.onClick.RemoveListener(CloseChat);
                closeChatButton.onClick.AddListener(CloseChat);
            }

            if (userChatTabButton != null)
            {
                userChatTabButton.onClick.RemoveListener(ShowPrivateUserTab);
                userChatTabButton.onClick.AddListener(ShowPrivateUserTab);
            }

            if (publicChatTabButton != null)
            {
                publicChatTabButton.onClick.RemoveListener(OpenPublicChat);
                publicChatTabButton.onClick.AddListener(OpenPublicChat);
            }
        }

        private void HandleMessageSent()
        {
            isSendingMessage = false;
            SetSendInteractable(true);
            SetStatus("");
            LoadActiveMessages(true);
        }

        private void HandleMessageSendFailed(string error, string originalMessage)
        {
            isSendingMessage = false;
            SetSendInteractable(true);
            if (messageInput != null)
            {
                messageInput.text = originalMessage;
            }

            SetStatus(error);
        }

        private void ShowPrivateUserTab()
        {
            activeChatMode = ChatMode.Private;
            StopRefreshRoutine();
            SetChatVisible(false);
            SetUserListVisible(true);
            SetActiveTab(ChatMode.Private);
            RefreshUserList();
        }

        private void SetActiveTab(ChatMode mode)
        {
            SetTabVisual(userChatTabButton, mode == ChatMode.Private);
            SetTabVisual(publicChatTabButton, mode == ChatMode.Public);
        }

        private static void SetTabVisual(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? new Color32(36, 51, 88, 255) : new Color32(36, 51, 88, 115);
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = Color.white;// : new Color32(24, 29, 39, 255);
            }
        }

        private void EnsurePublicChatUiExtensions()
        {
            if (userListPanel == null)
            {
                return;
            }

            if (userChatTabButton != null && publicChatTabButton != null)
            {
                BindButtons();
                SetActiveTab(activeChatMode);
                return;
            }

            Transform existingTabs = userListPanel.transform.Find("PUN_ChatModeTabs_Runtime");
            if (existingTabs != null)
            {
                AssignTabButtons(existingTabs);
                BindButtons();
                SetActiveTab(activeChatMode);
                return;
            }

            GameObject tabsObject = new GameObject("PUN_ChatModeTabs_Runtime", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tabsObject.transform.SetParent(userListPanel.transform, false);
            RectTransform tabsRect = tabsObject.GetComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0f, 1f);
            tabsRect.anchorMax = new Vector2(1f, 1f);
            tabsRect.pivot = new Vector2(0.5f, 1f);
            tabsRect.anchoredPosition = new Vector2(0f, -116f);
            tabsRect.sizeDelta = new Vector2(-32f, 36f);

            Image tabsBg = tabsObject.GetComponent<Image>();
            tabsBg.color = new Color32(235, 239, 246, 230);

            userChatTabButton = CreateRuntimeTabButton(tabsObject.transform, "UserChatTab", "User Chat", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 1f));
            publicChatTabButton = CreateRuntimeTabButton(tabsObject.transform, "AllChatTab", "All Chat", new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 1f));

            BindButtons();
            SetActiveTab(ChatMode.Private);
        }

        private void AssignTabButtons(Transform tabsRoot)
        {
            if (userChatTabButton == null)
            {
                Transform privateTab = tabsRoot.Find("UserChatTab");
                if (privateTab != null)
                {
                    userChatTabButton = privateTab.GetComponent<Button>();
                }
            }

            if (publicChatTabButton == null)
            {
                Transform publicTab = tabsRoot.Find("AllChatTab");
                if (publicTab != null)
                {
                    publicChatTabButton = publicTab.GetComponent<Button>();
                }
            }
        }

        private static Button CreateRuntimeTabButton(Transform parent, string name, string label, Vector2 offset, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
            rect.anchoredPosition += offset;

            Image image = obj.GetComponent<Image>();
            image.color = new Color32(235, 239, 246, 255);

            Button button = obj.GetComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(obj.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 2f);
            textRect.offsetMax = new Vector2(-6f, -2f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 13;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color32(24, 29, 39, 255);
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;

            return button;
        }

        private void RefreshVisibility()
        {
            bool shouldShow = showChatBtn && IsLocalPlayerReady() && SceneManager.GetActiveScene().buildIndex!=0;
            SetJoinedUiVisible(shouldShow);

            if (!shouldShow)
            {
                SetUserListVisible(false);
                SetChatVisible(false);
            }
        }

        private void SetJoinedUiVisible(bool visible)
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = visible ? 1f : 0f;
                rootCanvasGroup.interactable = visible;
                rootCanvasGroup.blocksRaycasts = visible;
            }

            if (chatToggleButton != null)
            {
                chatToggleButton.gameObject.SetActive(visible);
            }
        }

        private void RefreshUserList()
        {
            if (userListContent == null || userListItemPrefab == null)
            {
                return;
            }

            List<int> activeActorNumbers = new List<int>();

            if (IsLocalPlayerReady())
            {
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    if (player == null || player.IsLocal)
                    {
                        continue;
                    }

                    ResolvePlayerIdentity(player, out string displayName, out string userId);
                    activeActorNumbers.Add(player.ActorNumber);

                    if (!visiblePlayers.TryGetValue(player.ActorNumber, out PUN_ChatListItem item) || item == null)
                    {
                        item = Instantiate(userListItemPrefab, userListContent);
                        item.gameObject.SetActive(true);
                        visiblePlayers[player.ActorNumber] = item;
                    }

                    item.Bind(this, player, displayName, userId);
                    item.SetUnread(unreadUserIds.Contains(userId));
                }
            }

            List<int> removeActors = new List<int>();
            foreach (KeyValuePair<int, PUN_ChatListItem> pair in visiblePlayers)
            {
                if (!activeActorNumbers.Contains(pair.Key))
                {
                    if (pair.Value != null)
                    {
                        Destroy(pair.Value.gameObject);
                    }

                    removeActors.Add(pair.Key);
                }
            }

            foreach (int actorNumber in removeActors)
            {
                visiblePlayers.Remove(actorNumber);
            }

            if (userListStatusText != null)
            {
                if (!PhotonNetwork.InRoom)
                {
                    userListStatusText.text = "Join a room to see users";
                }
                else if (!IsLocalPlayerReady())
                {
                    userListStatusText.text = "Waiting for player...";
                }
                else if (visiblePlayers.Count == 0)
                {
                    userListStatusText.text = "No other users online";
                }
                else
                {
                    userListStatusText.text = "";
                }
            }
        }

        private void RefreshUnreadIndicators()
        {
            if (!TryGetLocalIdentity(out _, out string myUserId))
            {
                return;
            }

            foreach (PUN_ChatListItem item in visiblePlayers.Values)
            {
                if (item == null || string.IsNullOrEmpty(item.UserId))
                {
                    continue;
                }

                string otherUserId = item.UserId;
                if (otherUserId == activeUserId)
                {
                    continue;
                }

                string requestKey = FirestoreChatService.GetChatId(myUserId, otherUserId);
                if (!unreadRequestsInFlight.Add(requestKey))
                {
                    continue;
                }

                FirestoreChatService.LoadRecentMessages(
                    myUserId,
                    otherUserId,
                    messages => {
                        unreadRequestsInFlight.Remove(requestKey);
                        UpdateUnreadFromLatestMessage(myUserId, otherUserId, messages);
                    },
                    _ => unreadRequestsInFlight.Remove(requestKey),
                    1);
            }
        }

        private void UpdateUnreadFromLatestMessage(string myUserId, string otherUserId, ChatMessageData[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                unreadUserIds.Remove(otherUserId);
                UpdateUnreadIndicators();
                return;
            }

            ChatMessageData latest = messages[messages.Length - 1];
            if (latest == null || string.IsNullOrEmpty(latest.id))
            {
                UpdateUnreadIndicators();
                return;
            }

            if (latest.senderId == myUserId)
            {
                SaveLastReadMessageId(myUserId, otherUserId, latest.id);
                unreadUserIds.Remove(otherUserId);
                UpdateUnreadIndicators();
                return;
            }

            string lastReadId = GetLastReadMessageId(myUserId, otherUserId);
            if (latest.id != lastReadId)
            {
                unreadUserIds.Add(otherUserId);
            }
            else
            {
                unreadUserIds.Remove(otherUserId);
            }

            UpdateUnreadIndicators();
        }

        private void MarkMessagesRead(string myUserId, string otherUserId, ChatMessageData[] messages)
        {
            if (string.IsNullOrEmpty(otherUserId) || messages == null || messages.Length == 0)
            {
                unreadUserIds.Remove(otherUserId);
                UpdateUnreadIndicators();
                return;
            }

            ChatMessageData latest = messages[messages.Length - 1];
            if (latest != null && !string.IsNullOrEmpty(latest.id))
            {
                SaveLastReadMessageId(myUserId, otherUserId, latest.id);
            }

            unreadUserIds.Remove(otherUserId);
            UpdateUnreadIndicators();
        }

        private void UpdateUnreadIndicators()
        {
            int unreadCount = unreadUserIds.Count;
            if (toggleUnreadText != null)
            {
                toggleUnreadText.transform.parent.gameObject.SetActive(unreadCount > 0);
                toggleUnreadText.text = unreadCount > 9 ? "9+" : unreadCount.ToString();
            }

            foreach (PUN_ChatListItem item in visiblePlayers.Values)
            {
                if (item != null)
                {
                    item.SetUnread(unreadUserIds.Contains(item.UserId));
                }
            }
        }

        private static string GetLastReadMessageId(string myUserId, string otherUserId)
        {
            return PlayerPrefs.GetString(GetLastReadPrefsKey(myUserId, otherUserId), "");
        }

        private static void SaveLastReadMessageId(string myUserId, string otherUserId, string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                return;
            }

            PlayerPrefs.SetString(GetLastReadPrefsKey(myUserId, otherUserId), messageId);
            PlayerPrefs.Save();
        }

        private static string GetLastReadPrefsKey(string myUserId, string otherUserId)
        {
            return $"PUNChat_LastRead_{FirestoreChatService.GetChatId(myUserId, otherUserId)}";
        }

        private void LoadActiveMessages(bool scrollToBottom)
        {
            if (isLoadingMessages || string.IsNullOrEmpty(activeUserId))
            {
                return;
            }

            if (!TryGetLocalIdentity(out _, out string myUserId))
            {
                SetStatus("Login is required before chat can load.");
                return;
            }

            isLoadingMessages = true;
            if (activeChatMode == ChatMode.Public)
            {
                FirestoreChatService.LoadRecentPublicMessages(
                    messages => {
                        isLoadingMessages = false;
                        RenderMessages(messages, myUserId);
                        SetStatus("");
                        if (scrollToBottom)
                        {
                            StartCoroutine(ScrollToBottomNextFrame());
                        }
                    },
                    error => {
                        isLoadingMessages = false;
                        SetStatus(error);
                    },
                    messageLoadLimit);
                return;
            }

            FirestoreChatService.LoadRecentMessages(
                myUserId,
                activeUserId,
                messages => {
                    isLoadingMessages = false;
                    RenderMessages(messages, myUserId);
                    MarkMessagesRead(myUserId, activeUserId, messages);
                    SetStatus("");
                    if (scrollToBottom)
                    {
                        StartCoroutine(ScrollToBottomNextFrame());
                    }
                },
                error => {
                    isLoadingMessages = false;
                    SetStatus(error);
                },
                messageLoadLimit);
        }

        private void RenderMessages(ChatMessageData[] messages, string myUserId)
        {
            ClearMessages();
            renderedMessageIds.Clear();

            if (messages == null)
            {
                return;
            }

            foreach (ChatMessageData message in messages)
            {
                if (message == null)
                {
                    continue;
                }

                bool isMine = message.senderId == myUserId;
                PUN_ChatMessageItem prefab = activeChatMode == ChatMode.Public
                    ? incomingMessagePrefab
                    : isMine ? outgoingMessagePrefab : incomingMessagePrefab;
                if (prefab == null || messagesContent == null)
                {
                    continue;
                }

                PUN_ChatMessageItem item = Instantiate(prefab, messagesContent);
                item.gameObject.SetActive(true);
                if (activeChatMode == ChatMode.Public)
                {
                    item.BindPublic(message, isMine);
                }
                else
                {
                    item.Bind(message, isMine);
                }

                if (!string.IsNullOrEmpty(message.id))
                {
                    renderedMessageIds.Add(message.id);
                }
            }
        }

        private void ClearMessages()
        {
            if (messagesContent == null)
            {
                return;
            }

            for (int i = messagesContent.childCount - 1; i >= 0; i--)
            {
                Transform child = messagesContent.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (messagesScrollRect != null)
            {
                messagesScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void RestartRefreshRoutine()
        {
            StopRefreshRoutine();
            refreshRoutine = StartCoroutine(RefreshActiveChatRoutine());
        }

        private void StopRefreshRoutine()
        {
            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
                refreshRoutine = null;
            }
        }

        private IEnumerator RefreshActiveChatRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(Mathf.Max(1f, activeChatRefreshSeconds));
            while (!string.IsNullOrEmpty(activeUserId))
            {
                yield return wait;
                LoadActiveMessages(false);
            }
        }

        private void SetUserListVisible(bool visible)
        {
            if (userListPanel != null)
            {
                userListPanel.SetActive(visible);
            }
        }

        private void SetChatVisible(bool visible)
        {
            if (chatPanel != null)
            {
                chatPanel.SetActive(visible);
            }
        }

        private void SetStatus(string status)
        {
            if (chatStatusText != null)
            {
                chatStatusText.text = status ?? "";
            }
        }

        private void SetSendInteractable(bool interactable)
        {
            if (sendButton != null)
            {
                sendButton.interactable = interactable;
            }
        }

        private static bool IsLocalPlayerReady()
        {
            return PhotonNetwork.InRoom
                   && PUN_NetworkManager.nm != null
                   && PUN_NetworkManager.nm.myPlayer != null;
        }

        private void SyncLocalPhotonIdentityFromOnboarding()
        {
            if (!TryGetOnboardingIdentity(out string displayName, out string userId))
            {
                return;
            }

            string targetNickname = ChatIdentityUtility.BuildPunNickname(displayName, userId);
            if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.NickName != targetNickname)
            {
                PhotonNetwork.NickName = targetNickname;
            }
        }

        private bool TryGetLocalIdentity(out string displayName, out string userId)
        {
            if (TryGetOnboardingIdentity(out displayName, out userId))
            {
                return true;
            }

            if (PhotonNetwork.LocalPlayer != null)
            {
                ResolvePlayerIdentity(PhotonNetwork.LocalPlayer, out displayName, out userId);
                return !string.IsNullOrEmpty(userId);
            }

            displayName = "";
            userId = "";
            return false;
        }

        private bool TryGetOnboardingIdentity(out string displayName, out string userId)
        {
            userId = PlayerPrefs.GetString(PlayerPrefsUserId, "");
            displayName = PlayerPrefs.GetString(PlayerPrefsUsername, "");

            if (OnboardingManager.Instance != null)
            {
                if (!string.IsNullOrEmpty(OnboardingManager.Instance.CurrentUserId))
                {
                    userId = OnboardingManager.Instance.CurrentUserId;
                }

                if (!string.IsNullOrEmpty(OnboardingManager.Instance.CurrentUsername))
                {
                    displayName = OnboardingManager.Instance.CurrentUsername;
                }
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "User";
            }

            return !string.IsNullOrWhiteSpace(userId);
        }

        private static void ResolvePlayerIdentity(Player player, out string displayName, out string userId)
        {
            displayName = "";
            userId = "";

            if (player == null)
            {
                return;
            }

            if (ChatIdentityUtility.TryParsePunNickname(player.NickName, out displayName, out userId))
            {
                return;
            }

            displayName = string.IsNullOrWhiteSpace(player.NickName)
                ? $"Player {player.ActorNumber}"
                : player.NickName.Trim();

            if (!string.IsNullOrWhiteSpace(player.UserId))
            {
                userId = player.UserId.Trim();
            }
            else
            {
                userId = $"actor_{player.ActorNumber}";
            }
        }

        private static string GetInitials(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "U";
            }

            string[] parts = value.Trim().Split(' ');
            if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
            {
                return (parts[0][0].ToString() + parts[1][0]).ToUpperInvariant();
            }

            return value[0].ToString().ToUpperInvariant();
        }
    }

    public static class PUN_ChatRuntimeUiBuilder
    {
        public const int CurrentVersion = 2;

        private static readonly Color PanelColor = new Color32(248, 250, 252, 252);
        private static readonly Color HeaderColor = new Color32(255, 255, 255, 255);
        private static readonly Color SurfaceColor = new Color32(255, 255, 255, 255);
        private static readonly Color SoftSurfaceColor = new Color32(239, 242, 247, 255);
        private static readonly Color ButtonColor = new Color32(24, 90, 255, 255);
        private static readonly Color MutedButtonColor = new Color32(235, 239, 246, 255);
        private static readonly Color TextColor = new Color32(24, 29, 39, 255);
        private static readonly Color MutedTextColor = new Color32(102, 112, 133, 255);
        private static readonly Color BorderColor = new Color32(222, 226, 234, 255);

        public static void Build(PUN_ChatManager manager)
        {
            ClearGeneratedChildren(manager.transform);
            EnsureEventSystem();

            Canvas canvas = CreateCanvas(manager.transform);
            CanvasGroup rootCanvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();

            Button toggleButton = CreateButton(canvas.transform, "PUN_ChatButton", "Messages", new Vector2(30f, 30f), new Vector2(172f, 54f), TextAnchor.LowerLeft, ButtonColor, Color.white);
            TextMeshProUGUI toggleUnreadText = CreateUnreadBadge(toggleButton.transform, "UnreadBadge", new Vector2(-8f, -8f), TextAnchor.UpperRight);
            toggleUnreadText.transform.parent.gameObject.SetActive(false);

            GameObject userListPanel = CreatePanel(canvas.transform, "PUN_ChatUserListPanel_LeftDrawer", new Vector2(30f, 96f), new Vector2(440f, 690f), TextAnchor.LowerLeft);
            Image listHeader = CreateImage(userListPanel.transform, "Header", HeaderColor);
            SetRect(listHeader.rectTransform, Vector2.zero, new Vector2(440f, 112f), TextAnchor.UpperLeft);
            CreateDivider(userListPanel.transform, "HeaderDivider", new Vector2(0f, -112f), 440f, TextAnchor.UpperLeft);

            TextMeshProUGUI listTitle = CreateText(listHeader.transform, "Title", "Messages", 24, FontStyles.Bold, TextColor);
            SetRect(listTitle.rectTransform, new Vector2(20f, -14f), new Vector2(250f, 34f), TextAnchor.UpperLeft);

            TextMeshProUGUI listStatus = CreateText(listHeader.transform, "Status", "", 13, FontStyles.Normal, MutedTextColor);
            SetRect(listStatus.rectTransform, new Vector2(20f, -48f), new Vector2(390f, 22f), TextAnchor.UpperLeft);

            Image searchBox = CreateImage(listHeader.transform, "SearchVisual", SoftSurfaceColor);
            SetRect(searchBox.rectTransform, new Vector2(18f, -78f), new Vector2(404f, 28f), TextAnchor.UpperLeft);
            TextMeshProUGUI searchText = CreateText(searchBox.transform, "Text", "People online", 12, FontStyles.Normal, MutedTextColor);
            Stretch(searchText.rectTransform, new Vector2(12f, 3f), new Vector2(-12f, -3f));

            ScrollRect userScroll = CreateScrollView(userListPanel.transform, "UserScroll", new Vector2(16f, 16f), new Vector2(408f, 548f), TextAnchor.LowerLeft, out RectTransform userContent);
            VerticalLayoutGroup userLayout = userContent.gameObject.AddComponent<VerticalLayoutGroup>();
            userLayout.spacing = 10f;
            userLayout.childControlHeight = true;
            userLayout.childControlWidth = true;
            userLayout.childForceExpandHeight = false;
            userLayout.childForceExpandWidth = true;
            userContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            PUN_ChatListItem userItemPrefab = CreateUserItemPrefab(userListPanel.transform);

            GameObject chatPanel = CreatePanel(canvas.transform, "PUN_ChatPanel_LeftDrawer", new Vector2(30f, 96f), new Vector2(440f, 690f), TextAnchor.LowerLeft);
            Image chatHeader = CreateImage(chatPanel.transform, "Header", HeaderColor);
            SetRect(chatHeader.rectTransform, Vector2.zero, new Vector2(440f, 82f), TextAnchor.UpperLeft);
            CreateDivider(chatPanel.transform, "HeaderDivider", new Vector2(0f, -82f), 440f, TextAnchor.UpperLeft);

            Button closeButton = CreateButton(chatHeader.transform, "BackButton", "<", new Vector2(14f, -21f), new Vector2(40f, 40f), TextAnchor.UpperLeft, MutedButtonColor, TextColor);

            TextMeshProUGUI headerAvatar = CreateAvatar(chatHeader.transform, "Avatar", "U", new Vector2(64f, -17f), 48f);
            headerAvatar.transform.parent.name = "HeaderAvatar";

            TextMeshProUGUI chatHeaderText = CreateText(chatHeader.transform, "ChatHeaderText", "Chat", 18, FontStyles.Bold, TextColor);
            SetRect(chatHeaderText.rectTransform, new Vector2(122f, -18f), new Vector2(260f, 26f), TextAnchor.UpperLeft);

            TextMeshProUGUI chatStatusText = CreateText(chatHeader.transform, "ChatStatusText", "Online", 12, FontStyles.Normal, MutedTextColor);
            SetRect(chatStatusText.rectTransform, new Vector2(122f, -44f), new Vector2(260f, 20f), TextAnchor.UpperLeft);

            ScrollRect messageScroll = CreateScrollView(chatPanel.transform, "MessagesScroll", new Vector2(16f, -96f), new Vector2(408f, 510f), TextAnchor.UpperLeft, out RectTransform messagesContent);
            VerticalLayoutGroup messageLayout = messagesContent.gameObject.AddComponent<VerticalLayoutGroup>();
            messageLayout.spacing = 10f;
            messageLayout.childControlHeight = true;
            messageLayout.childControlWidth = true;
            messageLayout.childForceExpandHeight = false;
            messageLayout.childForceExpandWidth = true;
            messagesContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_InputField input = CreateInput(chatPanel.transform);
            Button sendButton = CreateButton(chatPanel.transform, "SendButton", "Send", new Vector2(-16f, 16f), new Vector2(82f, 44f), TextAnchor.LowerRight, ButtonColor, Color.white);

            GameObject prefabRoot = new GameObject("PUN_ChatMessagePrefabs", typeof(RectTransform));
            prefabRoot.transform.SetParent(canvas.transform, false);
            PUN_ChatMessageItem outgoingPrefab = CreateMessagePrefab(prefabRoot.transform, "PUN_ChatMessage_Right_Sent_Prefab", true);
            PUN_ChatMessageItem incomingPrefab = CreateMessagePrefab(prefabRoot.transform, "PUN_ChatMessage_Left_Received_Prefab", false);

            userListPanel.SetActive(false);
            chatPanel.SetActive(false);
            userItemPrefab.gameObject.SetActive(false);
            prefabRoot.SetActive(false);

            manager.AssignReferences(
                canvas,
                rootCanvasGroup,
                toggleButton,
                toggleUnreadText,
                userListPanel,
                userContent,
                listStatus,
                userItemPrefab,
                chatPanel,
                chatHeaderText,
                headerAvatar,
                chatStatusText,
                messageScroll,
                messagesContent,
                input,
                sendButton,
                closeButton,
                outgoingPrefab,
                incomingPrefab);
            manager.SetGeneratedUiVersion(CurrentVersion);
        }

        private static void ClearGeneratedChildren(Transform managerTransform)
        {
            for (int i = managerTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = managerTransform.GetChild(i);
                if (child == null || !child.name.StartsWith("PUN_Chat"))
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("PUN_ChatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 998f);
            scaler.matchWidthOrHeight = 0f;

            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(eventSystemObject);
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor)
        {
            Image image = CreateImage(parent, name, PanelColor);
            SetRect(image.rectTransform, anchoredPosition, size, anchor);
            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(15, 23, 42, 38);
            shadow.effectDistance = new Vector2(0f, -4f);
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(1f, -1f);
            return image.gameObject;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor, Color color, Color labelColor)
        {
            Image image = CreateImage(parent, name, color);
            SetRect(image.rectTransform, anchoredPosition, size, anchor);

            Button button = image.gameObject.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.12f;
            colors.pressedColor = color * 0.86f;
            colors.selectedColor = color;
            button.colors = colors;

            TextMeshProUGUI text = CreateText(image.transform, "Label", label, 16, FontStyles.Bold, labelColor);
            Stretch(text.rectTransform, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static TextMeshProUGUI CreateAvatar(Transform parent, string name, string initials, Vector2 anchoredPosition, float size)
        {
            Image image = CreateImage(parent, name, new Color32(224, 233, 255, 255));
            SetRect(image.rectTransform, anchoredPosition, new Vector2(size, size), TextAnchor.UpperLeft);

            TextMeshProUGUI text = CreateText(image.transform, "Initials", initials, Mathf.RoundToInt(size * 0.36f), FontStyles.Bold, ButtonColor);
            Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string value, int fontSize, FontStyles style, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            return text;
        }

        private static ScrollRect CreateScrollView(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor, out RectTransform content)
        {
            Image viewportImage = CreateImage(parent, name, new Color32(248, 250, 252, 255));
            SetRect(viewportImage.rectTransform, anchoredPosition, size, anchor);

            Mask mask = viewportImage.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewportImage.transform, false);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            ScrollRect scroll = viewportImage.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportImage.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        private static TMP_InputField CreateInput(Transform parent)
        {
            Image inputBar = CreateImage(parent, "InputBar", HeaderColor);
            SetRect(inputBar.rectTransform, new Vector2(0f, 0f), new Vector2(440f, 76f), TextAnchor.LowerLeft);

            Image image = CreateImage(inputBar.transform, "MessageInput", SoftSurfaceColor);
            SetRect(image.rectTransform, new Vector2(16f, 16f), new Vector2(316f, 44f), TextAnchor.LowerLeft);

            TMP_InputField input = image.gameObject.AddComponent<TMP_InputField>();
            TextMeshProUGUI text = CreateText(image.transform, "Text", "", 15, FontStyles.Normal, TextColor);
            Stretch(text.rectTransform, new Vector2(14f, 8f), new Vector2(-14f, -8f));
            TextMeshProUGUI placeholder = CreateText(image.transform, "Placeholder", "Type a message...", 15, FontStyles.Normal, MutedTextColor);
            Stretch(placeholder.rectTransform, new Vector2(14f, 8f), new Vector2(-14f, -8f));
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = FirestoreChatService.MaxMessageLength;
            return input;
        }

        private static PUN_ChatListItem CreateUserItemPrefab(Transform parent)
        {
            Image image = CreateImage(parent, "PUN_ChatListItemPrefab", SurfaceColor);
            SetRect(image.rectTransform, Vector2.zero, new Vector2(408f, 82f), TextAnchor.UpperLeft);
            image.gameObject.AddComponent<LayoutElement>().preferredHeight = 82f;
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(1f, -1f);
            Button button = image.gameObject.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            TextMeshProUGUI avatarText = CreateAvatar(image.transform, "Avatar", "U", new Vector2(16f, -17f), 48f);

            Image indicator = CreateImage(image.transform, "OnlineIndicator", new Color32(20, 184, 116, 255));
            SetRect(indicator.rectTransform, new Vector2(52f, -53f), new Vector2(11f, 11f), TextAnchor.UpperLeft);

            TextMeshProUGUI nameText = CreateText(image.transform, "DisplayName", "Player", 15, FontStyles.Bold, TextColor);
            SetRect(nameText.rectTransform, new Vector2(78f, -17f), new Vector2(252f, 24f), TextAnchor.UpperLeft);

            TextMeshProUGUI idText = CreateText(image.transform, "UserId", "Online", 12, FontStyles.Normal, MutedTextColor);
            SetRect(idText.rectTransform, new Vector2(78f, -43f), new Vector2(252f, 20f), TextAnchor.UpperLeft);

            TextMeshProUGUI unreadText = CreateUnreadBadge(image.transform, "UnreadBadge", new Vector2(-14f, -18f), TextAnchor.UpperRight);
            unreadText.text = "New";
            unreadText.fontSize = 10;
            SetRect(unreadText.transform.parent.GetComponent<RectTransform>(), new Vector2(-14f, -18f), new Vector2(46f, 22f), TextAnchor.UpperRight);
            unreadText.gameObject.transform.parent.gameObject.SetActive(false);

            PUN_ChatListItem item = image.gameObject.AddComponent<PUN_ChatListItem>();
            item.Configure(button, nameText, idText, avatarText, indicator.gameObject, unreadText.transform.parent.gameObject, unreadText);
            button.transition = Selectable.Transition.ColorTint;
            return item;
        }

        private static TextMeshProUGUI CreateUnreadBadge(Transform parent, string name, Vector2 anchoredPosition, TextAnchor anchor)
        {
            Image badgeImage = CreateImage(parent, name, new Color32(235, 56, 72, 255));
            SetRect(badgeImage.rectTransform, anchoredPosition, new Vector2(28f, 28f), anchor);

            TextMeshProUGUI badgeText = CreateText(badgeImage.transform, "Text", "1", 12, FontStyles.Bold, Color.white);
            Stretch(badgeText.rectTransform, Vector2.zero, Vector2.zero);
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.enableWordWrapping = false;
            badgeText.overflowMode = TextOverflowModes.Ellipsis;
            return badgeText;
        }

        private static PUN_ChatMessageItem CreateMessagePrefab(Transform parent, string name, bool outgoing)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            LayoutElement rootLayout = root.GetComponent<LayoutElement>();
            rootLayout.minHeight = 50f;
            HorizontalLayoutGroup row = root.GetComponent<HorizontalLayoutGroup>();
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.padding = new RectOffset(outgoing ? 96 : 4, outgoing ? 4 : 96, 0, 0);
            row.childAlignment = outgoing ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;

            Image bubble = CreateImage(root.transform, "Bubble", outgoing ? ButtonColor : SoftSurfaceColor);
            bubble.gameObject.AddComponent<LayoutElement>().preferredWidth = 300f;
            Shadow bubbleShadow = bubble.gameObject.AddComponent<Shadow>();
            bubbleShadow.effectColor = new Color32(15, 23, 42, outgoing ? (byte)24 : (byte)12);
            bubbleShadow.effectDistance = new Vector2(0f, -1f);
            VerticalLayoutGroup layout = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 9, 8);
            layout.spacing = 4f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            TextMeshProUGUI messageText = CreateText(bubble.transform, "MessageText", "", 14, FontStyles.Normal, TextColor);
            messageText.overflowMode = TextOverflowModes.Overflow;

            TextMeshProUGUI metaText = CreateText(bubble.transform, "MetaText", "", 10, FontStyles.Normal, new Color32(210, 216, 230, 220));
            metaText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            PUN_ChatMessageItem item = root.AddComponent<PUN_ChatMessageItem>();
            item.Configure(messageText, metaText, bubble);
            return item;
        }

        private static Image CreateDivider(Transform parent, string name, Vector2 anchoredPosition, float width, TextAnchor anchor)
        {
            Image divider = CreateImage(parent, name, BorderColor);
            SetRect(divider.rectTransform, anchoredPosition, new Vector2(width, 1f), anchor);
            return divider;
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor)
        {
            Vector2 min;
            Vector2 max;
            Vector2 pivot;

            switch (anchor)
            {
                case TextAnchor.LowerLeft:
                    min = max = pivot = new Vector2(0f, 0f);
                    break;
                case TextAnchor.LowerRight:
                    min = max = pivot = new Vector2(1f, 0f);
                    break;
                case TextAnchor.UpperRight:
                    min = max = pivot = new Vector2(1f, 1f);
                    break;
                default:
                    min = max = pivot = new Vector2(0f, 1f);
                    break;
            }

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
