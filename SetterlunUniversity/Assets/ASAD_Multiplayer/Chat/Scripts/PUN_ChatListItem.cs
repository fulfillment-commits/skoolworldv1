using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASAD_Multiplyer.Chat
{
    public class PUN_ChatListItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI displayNameText;
        [SerializeField] private TextMeshProUGUI userIdText;
        [SerializeField] private TextMeshProUGUI avatarText;
        [SerializeField] private GameObject onlineIndicator;
        [SerializeField] private GameObject unreadIndicator;
        [SerializeField] private TextMeshProUGUI unreadText;

        public string UserId { get; private set; }
        public string DisplayName { get; private set; }
        public int ActorNumber { get; private set; }
        public Player PhotonPlayer { get; private set; }

        private PUN_ChatManager manager;

        public void Configure(
            Button assignedButton,
            TextMeshProUGUI assignedDisplayNameText,
            TextMeshProUGUI assignedUserIdText,
            TextMeshProUGUI assignedAvatarText,
            GameObject assignedOnlineIndicator,
            GameObject assignedUnreadIndicator = null,
            TextMeshProUGUI assignedUnreadText = null)
        {
            button = assignedButton;
            displayNameText = assignedDisplayNameText;
            userIdText = assignedUserIdText;
            avatarText = assignedAvatarText;
            onlineIndicator = assignedOnlineIndicator;
            unreadIndicator = assignedUnreadIndicator;
            unreadText = assignedUnreadText;
        }

        public void Bind(PUN_ChatManager owner, Player player, string displayName, string userId)
        {
            manager = owner;
            PhotonPlayer = player;
            ActorNumber = player != null ? player.ActorNumber : 0;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"Player {ActorNumber}" : displayName.Trim();
            UserId = string.IsNullOrWhiteSpace(userId) ? $"actor_{ActorNumber}" : userId.Trim();

            if (displayNameText != null)
            {
                displayNameText.text = DisplayName;
            }

            if (userIdText != null)
            {
                userIdText.text = "Online";
            }

            if (avatarText != null)
            {
                avatarText.text = GetInitials(DisplayName);
            }

            if (onlineIndicator != null)
            {
                onlineIndicator.SetActive(true);
            }

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }
        }

        public void SetUnread(bool unread)
        {
            if (unreadIndicator != null)
            {
                unreadIndicator.SetActive(unread);
            }

            if (unreadText != null)
            {
                unreadText.text = unread ? "New" : "";
            }
        }

        private void Reset()
        {
            button = GetComponent<Button>();
            TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (labels.Length > 0)
            {
                displayNameText = labels[0];
            }

            if (labels.Length > 1)
            {
                userIdText = labels[1];
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            manager?.OpenChat(this);
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
}
