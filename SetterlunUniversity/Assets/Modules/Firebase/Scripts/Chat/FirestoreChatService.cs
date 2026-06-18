using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class FirestoreChatService
{
    public const int DefaultMessageLimit = 50;
    public const int DefaultChatIndexLimit = 30;
    public const int MaxMessageLength = 1000;
    public const string PublicChatId = "global";
    private const string PublicReceiverId = "public";

    public static string GetChatId(string uidA, string uidB)
    {
        return ChatIdentityUtility.GetChatId(uidA, uidB);
    }

    public static string GetChatPath(string chatId)
    {
        return $"chats/{ChatIdentityUtility.SanitizeFirestoreId(chatId)}";
    }

    public static string GetMessagesPath(string chatId)
    {
        return $"{GetChatPath(chatId)}/messages";
    }

    public static string GetMessagePath(string chatId, string messageId)
    {
        return $"{GetMessagesPath(chatId)}/{ChatIdentityUtility.SanitizeFirestoreId(messageId)}";
    }

    public static string GetChatIndexPath(string userId, string chatId)
    {
        return $"users/{ChatIdentityUtility.SanitizeFirestoreId(userId)}/chatIndex/{ChatIdentityUtility.SanitizeFirestoreId(chatId)}";
    }

    public static string GetPublicChatPath(string publicChatId = PublicChatId)
    {
        return $"publicChats/{ChatIdentityUtility.SanitizeFirestoreId(publicChatId)}";
    }

    public static string GetPublicMessagesPath(string publicChatId = PublicChatId)
    {
        return $"{GetPublicChatPath(publicChatId)}/messages";
    }

    public static string GetPublicMessagePath(string messageId, string publicChatId = PublicChatId)
    {
        return $"{GetPublicMessagesPath(publicChatId)}/{ChatIdentityUtility.SanitizeFirestoreId(messageId)}";
    }

    public static void CreateOrOpenChat(
        string myUid,
        string otherUid,
        string myDisplayName,
        string otherDisplayName,
        Action<ChatThreadData> onSuccess,
        Action<string> onError = null)
    {
        if (!ValidateUsers(myUid, otherUid, onError))
        {
            return;
        }

        string now = UtcTimestamp();
        string chatId = GetChatId(myUid, otherUid);
        ChatThreadData thread = new ChatThreadData {
            chatId = chatId,
            participantA = ChatIdentityUtility.SanitizeFirestoreId(myUid),
            participantB = ChatIdentityUtility.SanitizeFirestoreId(otherUid),
            participantAName = string.IsNullOrWhiteSpace(myDisplayName) ? "Me" : myDisplayName.Trim(),
            participantBName = string.IsNullOrWhiteSpace(otherDisplayName) ? "User" : otherDisplayName.Trim(),
            createdAt = now,
            updatedAt = now,
            lastMessage = "",
            lastMessageAt = "",
            lastSenderId = ""
        };

        string chatPath = GetChatPath(chatId);
        FirebaseManager.GetData<ChatThreadData>(chatPath, existingThread => {
            if (existingThread != null && !string.IsNullOrEmpty(existingThread.chatId))
            {
                onSuccess?.Invoke(existingThread);
                return;
            }

            SaveNewThread(chatPath, thread, onSuccess, onError);
        }, _ => SaveNewThread(chatPath, thread, onSuccess, onError));
    }

    private static void SaveNewThread(string chatPath, ChatThreadData thread, Action<ChatThreadData> onSuccess, Action<string> onError)
    {
        FirebaseManager.SetData(chatPath, thread, (success, message) => {
            if (!success)
            {
                onError?.Invoke(message);
                return;
            }

            onSuccess?.Invoke(thread);
        });
    }

    public static void SendMessage(
        string myUid,
        string otherUid,
        string text,
        Action<ChatMessageData> onSuccess,
        Action<string> onError = null,
        string myDisplayName = "",
        string otherDisplayName = "",
        bool updateReceiverIndex = true)
    {
        if (!ValidateUsers(myUid, otherUid, onError))
        {
            return;
        }

        string cleanText = NormalizeMessage(text);
        if (string.IsNullOrEmpty(cleanText))
        {
            onError?.Invoke("Message is empty.");
            return;
        }

        if (cleanText.Length > MaxMessageLength)
        {
            onError?.Invoke($"Message is too long. Max length is {MaxMessageLength} characters.");
            return;
        }

        string chatId = GetChatId(myUid, otherUid);
        string now = UtcTimestamp();
        string messageId = CreateMessageId(myUid);

        ChatMessageData message = new ChatMessageData {
            id = messageId,
            chatId = chatId,
            senderId = ChatIdentityUtility.SanitizeFirestoreId(myUid),
            receiverId = ChatIdentityUtility.SanitizeFirestoreId(otherUid),
            senderDisplayName = string.IsNullOrWhiteSpace(myDisplayName) ? "Me" : myDisplayName.Trim(),
            text = cleanText,
            clientCreatedAt = now,
            status = "sent"
        };

        ChatThreadData thread = new ChatThreadData {
            chatId = chatId,
            participantA = ChatIdentityUtility.SanitizeFirestoreId(myUid),
            participantB = ChatIdentityUtility.SanitizeFirestoreId(otherUid),
            participantAName = string.IsNullOrWhiteSpace(myDisplayName) ? "Me" : myDisplayName.Trim(),
            participantBName = string.IsNullOrWhiteSpace(otherDisplayName) ? "User" : otherDisplayName.Trim(),
            createdAt = now,
            updatedAt = now,
            lastMessage = cleanText,
            lastMessageAt = now,
            lastSenderId = ChatIdentityUtility.SanitizeFirestoreId(myUid)
        };

        string chatPath = GetChatPath(chatId);
        FirebaseManager.GetData<ChatThreadData>(chatPath, existingThread => {
            if (existingThread != null && !string.IsNullOrEmpty(existingThread.createdAt))
            {
                thread.createdAt = existingThread.createdAt;
            }

            SaveMessageThread(chatPath, thread, message, myUid, otherUid, myDisplayName, otherDisplayName, cleanText, now, updateReceiverIndex, onSuccess, onError);
        }, _ => SaveMessageThread(chatPath, thread, message, myUid, otherUid, myDisplayName, otherDisplayName, cleanText, now, updateReceiverIndex, onSuccess, onError));
    }

    private static void SaveMessageThread(
        string chatPath,
        ChatThreadData thread,
        ChatMessageData message,
        string myUid,
        string otherUid,
        string myDisplayName,
        string otherDisplayName,
        string cleanText,
        string timestamp,
        bool updateReceiverIndex,
        Action<ChatMessageData> onSuccess,
        Action<string> onError)
    {
        FirebaseManager.SetData(chatPath, thread, (threadSaved, threadError) => {
            if (!threadSaved)
            {
                onError?.Invoke(threadError);
                return;
            }

            FirebaseManager.SetData(GetMessagePath(thread.chatId, message.id), message, (messageSaved, messageError) => {
                if (!messageSaved)
                {
                    onError?.Invoke(messageError);
                    return;
                }

                UpdateChatIndexes(myUid, otherUid, myDisplayName, otherDisplayName, thread.chatId, cleanText, timestamp, updateReceiverIndex, (indexesSaved, indexError) => {
                    if (!indexesSaved)
                    {
                        Debug.LogWarning($"[FirestoreChatService] Message saved, but chat index update failed: {indexError}");
                    }

                    onSuccess?.Invoke(message);
                });
            });
        });
    }

    public static void LoadRecentMessages(
        string myUid,
        string otherUid,
        Action<ChatMessageData[]> onSuccess,
        Action<string> onError = null,
        int limit = DefaultMessageLimit)
    {
        if (!ValidateUsers(myUid, otherUid, onError))
        {
            return;
        }

        string chatId = GetChatId(myUid, otherUid);
        int safeLimit = Mathf.Clamp(limit, 1, DefaultMessageLimit);

        FirebaseManager.GetCollectionOrdered(GetMessagesPath(chatId), "clientCreatedAt", true, safeLimit, response => {
            List<ChatMessageData> messages = new List<ChatMessageData>();
            if (response.items != null)
            {
                foreach (FirestoreCollectionItem item in response.items)
                {
                    if (string.IsNullOrEmpty(item.data))
                    {
                        continue;
                    }

                    ChatMessageData message = JsonUtility.FromJson<ChatMessageData>(item.data);
                    message.id = item.id;
                    messages.Add(message);
                }
            }

            messages.Sort((a, b) => string.CompareOrdinal(a.clientCreatedAt, b.clientCreatedAt));
            onSuccess?.Invoke(messages.ToArray());
        }, onError);
    }

    public static void LoadChatIndex(
        string myUid,
        Action<ChatIndexData[]> onSuccess,
        Action<string> onError = null,
        int limit = DefaultChatIndexLimit)
    {
        if (string.IsNullOrWhiteSpace(myUid))
        {
            onError?.Invoke("Local Firebase UID is empty.");
            return;
        }

        int safeLimit = Mathf.Clamp(limit, 1, 100);
        string path = $"users/{ChatIdentityUtility.SanitizeFirestoreId(myUid)}/chatIndex";

        FirebaseManager.GetCollectionOrdered(path, "lastMessageAt", true, safeLimit, response => {
            List<ChatIndexData> chats = new List<ChatIndexData>();
            if (response.items != null)
            {
                foreach (FirestoreCollectionItem item in response.items)
                {
                    if (string.IsNullOrEmpty(item.data))
                    {
                        continue;
                    }

                    ChatIndexData chatIndex = JsonUtility.FromJson<ChatIndexData>(item.data);
                    if (string.IsNullOrEmpty(chatIndex.chatId))
                    {
                        chatIndex.chatId = item.id;
                    }

                    chats.Add(chatIndex);
                }
            }

            chats.Sort((a, b) => string.CompareOrdinal(b.lastMessageAt, a.lastMessageAt));
            onSuccess?.Invoke(chats.ToArray());
        }, onError);
    }

    public static void LoadRecentMessagesFromPunNickname(
        string myUid,
        string otherPunNickname,
        Action<ChatMessageData[]> onSuccess,
        Action<string> onError = null,
        int limit = DefaultMessageLimit)
    {
        if (!ChatIdentityUtility.TryParsePunNickname(otherPunNickname, out _, out string otherUid))
        {
            onError?.Invoke("Other player's PUN nickname does not contain a Firebase UID.");
            return;
        }

        LoadRecentMessages(myUid, otherUid, onSuccess, onError, limit);
    }

    public static void SendMessageFromPunNickname(
        string myUid,
        string myDisplayName,
        string otherPunNickname,
        string text,
        Action<ChatMessageData> onSuccess,
        Action<string> onError = null)
    {
        if (!ChatIdentityUtility.TryParsePunNickname(otherPunNickname, out string otherDisplayName, out string otherUid))
        {
            onError?.Invoke("Other player's PUN nickname does not contain a Firebase UID.");
            return;
        }

        SendMessage(myUid, otherUid, text, onSuccess, onError, myDisplayName, otherDisplayName);
    }

    public static void SendPublicMessage(
        string myUid,
        string myDisplayName,
        string text,
        Action<ChatMessageData> onSuccess,
        Action<string> onError = null,
        string publicChatId = PublicChatId)
    {
        if (string.IsNullOrWhiteSpace(myUid))
        {
            onError?.Invoke("Local Firebase UID is empty.");
            return;
        }

        string cleanText = NormalizeMessage(text);
        if (string.IsNullOrEmpty(cleanText))
        {
            onError?.Invoke("Message is empty.");
            return;
        }

        if (cleanText.Length > MaxMessageLength)
        {
            onError?.Invoke($"Message is too long. Max length is {MaxMessageLength} characters.");
            return;
        }

        string safePublicChatId = ChatIdentityUtility.SanitizeFirestoreId(publicChatId);
        string now = UtcTimestamp();
        string messageId = CreateMessageId(myUid);
        ChatMessageData message = new ChatMessageData {
            id = messageId,
            chatId = safePublicChatId,
            senderId = ChatIdentityUtility.SanitizeFirestoreId(myUid),
            receiverId = PublicReceiverId,
            senderDisplayName = string.IsNullOrWhiteSpace(myDisplayName) ? "User" : myDisplayName.Trim(),
            text = cleanText,
            clientCreatedAt = now,
            status = "sent"
        };

        PublicChatThreadData thread = new PublicChatThreadData {
            chatId = safePublicChatId,
            title = "All Chat",
            updatedAt = now,
            lastMessage = cleanText,
            lastMessageAt = now,
            lastSenderId = ChatIdentityUtility.SanitizeFirestoreId(myUid),
            lastSenderDisplayName = message.senderDisplayName
        };

        FirebaseManager.SetData(GetPublicChatPath(safePublicChatId), thread, (threadSaved, threadError) => {
            if (!threadSaved)
            {
                onError?.Invoke(threadError);
                return;
            }

            FirebaseManager.SetData(GetPublicMessagePath(message.id, safePublicChatId), message, (messageSaved, messageError) => {
                if (!messageSaved)
                {
                    onError?.Invoke(messageError);
                    return;
                }

                onSuccess?.Invoke(message);
            });
        });
    }

    public static void LoadRecentPublicMessages(
        Action<ChatMessageData[]> onSuccess,
        Action<string> onError = null,
        int limit = DefaultMessageLimit,
        string publicChatId = PublicChatId)
    {
        int safeLimit = Mathf.Clamp(limit, 1, DefaultMessageLimit);
        string safePublicChatId = ChatIdentityUtility.SanitizeFirestoreId(publicChatId);

        FirebaseManager.GetCollectionOrdered(GetPublicMessagesPath(safePublicChatId), "clientCreatedAt", true, safeLimit, response => {
            List<ChatMessageData> messages = new List<ChatMessageData>();
            if (response.items != null)
            {
                foreach (FirestoreCollectionItem item in response.items)
                {
                    if (string.IsNullOrEmpty(item.data))
                    {
                        continue;
                    }

                    ChatMessageData message = JsonUtility.FromJson<ChatMessageData>(item.data);
                    message.id = item.id;
                    messages.Add(message);
                }
            }

            messages.Sort((a, b) => string.CompareOrdinal(a.clientCreatedAt, b.clientCreatedAt));
            onSuccess?.Invoke(messages.ToArray());
        }, onError);
    }

    private static void UpdateChatIndexes(
        string myUid,
        string otherUid,
        string myDisplayName,
        string otherDisplayName,
        string chatId,
        string lastMessage,
        string lastMessageAt,
        bool updateReceiverIndex,
        Action<bool, string> callback)
    {
        ChatIndexData myIndex = new ChatIndexData {
            chatId = chatId,
            otherUserId = ChatIdentityUtility.SanitizeFirestoreId(otherUid),
            otherDisplayName = string.IsNullOrWhiteSpace(otherDisplayName) ? "User" : otherDisplayName.Trim(),
            lastMessage = lastMessage,
            lastMessageAt = lastMessageAt,
            lastSenderId = ChatIdentityUtility.SanitizeFirestoreId(myUid),
            unreadCount = 0
        };

        FirebaseManager.SetData(GetChatIndexPath(myUid, chatId), myIndex, (mySaved, myError) => {
            if (!mySaved)
            {
                callback?.Invoke(false, myError);
                return;
            }

            if (!updateReceiverIndex)
            {
                callback?.Invoke(true, "");
                return;
            }

            ChatIndexData receiverIndex = new ChatIndexData {
                chatId = chatId,
                otherUserId = ChatIdentityUtility.SanitizeFirestoreId(myUid),
                otherDisplayName = string.IsNullOrWhiteSpace(myDisplayName) ? "User" : myDisplayName.Trim(),
                lastMessage = lastMessage,
                lastMessageAt = lastMessageAt,
                lastSenderId = ChatIdentityUtility.SanitizeFirestoreId(myUid),
                unreadCount = 1
            };

            FirebaseManager.SetData(GetChatIndexPath(otherUid, chatId), receiverIndex, callback);
        });
    }

    private static bool ValidateUsers(string myUid, string otherUid, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(myUid))
        {
            onError?.Invoke("Local Firebase UID is empty.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(otherUid))
        {
            onError?.Invoke("Other Firebase UID is empty.");
            return false;
        }

        if (string.Equals(myUid.Trim(), otherUid.Trim(), StringComparison.Ordinal))
        {
            onError?.Invoke("Cannot open a private chat with the same user.");
            return false;
        }

        return true;
    }

    private static string NormalizeMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        return text.Trim();
    }

    private static string UtcTimestamp()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
    }

    private static string CreateMessageId(string senderUid)
    {
        string timePrefix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        string randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"{timePrefix}_{ChatIdentityUtility.SanitizeFirestoreId(senderUid)}_{randomSuffix}";
    }
}

[Serializable]
public class ChatThreadData
{
    public string chatId;
    public string participantA;
    public string participantB;
    public string participantAName;
    public string participantBName;
    public string createdAt;
    public string updatedAt;
    public string lastMessage;
    public string lastMessageAt;
    public string lastSenderId;
}

[Serializable]
public class PublicChatThreadData
{
    public string chatId;
    public string title;
    public string updatedAt;
    public string lastMessage;
    public string lastMessageAt;
    public string lastSenderId;
    public string lastSenderDisplayName;
}

[Serializable]
public class ChatMessageData
{
    public string id;
    public string chatId;
    public string senderId;
    public string receiverId;
    public string senderDisplayName;
    public string text;
    public string clientCreatedAt;
    public string status;
}

[Serializable]
public class ChatIndexData
{
    public string chatId;
    public string otherUserId;
    public string otherDisplayName;
    public string lastMessage;
    public string lastMessageAt;
    public string lastSenderId;
    public int unreadCount;
}
