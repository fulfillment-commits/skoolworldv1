using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASAD_Multiplyer.Chat
{
    public class PUN_ChatMessageItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI metaText;
        [SerializeField] private Image bubbleImage;

        public void Configure(TextMeshProUGUI assignedMessageText, TextMeshProUGUI assignedMetaText, Image assignedBubbleImage)
        {
            messageText = assignedMessageText;
            metaText = assignedMetaText;
            bubbleImage = assignedBubbleImage;
        }

        public void Bind(ChatMessageData message, bool isMine)
        {
            if (messageText != null)
            {
                messageText.text = message != null ? message.text : "";
                messageText.color = isMine ? Color.white : new Color32(28, 33, 45, 255);
            }

            if (metaText != null)
            {
                string sender = isMine ? "You" : (message != null && !string.IsNullOrEmpty(message.senderDisplayName) ? message.senderDisplayName : "User");
                string time = message != null ? FormatTime(message.clientCreatedAt) : "";
                metaText.text = string.IsNullOrEmpty(time) ? sender : $"{sender}  {time}";
                metaText.color = isMine ? new Color32(220, 232, 255, 230) : new Color32(102, 112, 133, 230);
                metaText.alignment = isMine ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            }

            if (bubbleImage != null)
            {
                bubbleImage.color = isMine
                    ? new Color32(28, 92, 255, 245)
                    : new Color32(239, 242, 247, 255);
            }
        }

        private static string FormatTime(string timestamp)
        {
            if (string.IsNullOrEmpty(timestamp))
            {
                return "";
            }

            if (System.DateTime.TryParse(timestamp, out System.DateTime parsed))
            {
                return parsed.ToLocalTime().ToString("HH:mm");
            }

            return "";
        }
    }
}
