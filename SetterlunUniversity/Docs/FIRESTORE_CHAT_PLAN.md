# Firestore Private Chat Plan

This document describes the recommended plan for adding one-to-one user chat to the Unity WebGL multiplayer project using Firebase Auth, Firestore, and PUN player identity.

## Goal

Add private chat between two authenticated users.

The chat UI can be opened from another visible PUN player. The game extracts the other user's Firebase UID from their PUN nickname, builds a stable conversation ID using both UIDs, loads messages from Firestore, and saves new messages to the same conversation.

## Current Project Context

The project already has:

- Firebase Auth in WebGL through `Assets/Plugins/WebGL/FirebaseBridge.jslib`.
- Generic Firestore set/update/get support through `FirebaseManager`.
- Collection loading support in the JavaScript bridge through `Firebase_Firestore_GetCollection`.
- PUN player names currently set through `PhotonNetwork.NickName`.
- Existing commented intent to use a nickname format like:

```text
username$firebaseUid$profileImageUrl$roomId
```

For chat, the most important field is the Firebase UID. The display name can change, but UID must not.

## Recommended Identity Format

Use this PUN nickname shape:

```text
displayName$firebaseUid
```

If you need more fields later:

```text
displayName$firebaseUid$avatarUrl$roomId
```

Rules:

- `firebaseUid` must come from Firebase Auth after login.
- Never use random PUN nicknames for persistent chat identity.
- `displayName` is only for UI.
- `firebaseUid` is the source of truth for loading/saving chat.

Helper idea:

```csharp
string GetUidFromPunName(string nickname)
{
    string[] parts = nickname.Split('$');
    return parts.Length > 1 ? parts[1] : "";
}
```

## Conversation ID

Use a deterministic conversation ID based on both user IDs.

Example:

```text
chatId = uidA < uidB ? uidA + "_" + uidB : uidB + "_" + uidA
```

This ensures:

- User A talking to User B uses the same document as User B talking to User A.
- No duplicate threads like `A_B` and `B_A`.
- Firestore rules can verify both users belong to the chat.

## Firestore Data Model

Recommended schema:

```text
chats/{chatId}
chats/{chatId}/messages/{messageId}
users/{uid}/chatIndex/{chatId}
```

### Chat Document

Path:

```text
chats/{chatId}
```

Data:

```json
{
  "participants": ["uidA", "uidB"],
  "participantMap": {
    "uidA": true,
    "uidB": true
  },
  "createdAt": "serverTimestamp",
  "updatedAt": "serverTimestamp",
  "lastMessage": "Hello",
  "lastMessageAt": "serverTimestamp",
  "lastSenderId": "uidA"
}
```

Why both `participants` and `participantMap`?

- `participants` is useful for display/debugging.
- `participantMap.{uid} == true` is easier and safer in Firestore security rules.

### Message Document

Path:

```text
chats/{chatId}/messages/{messageId}
```

Data:

```json
{
  "senderId": "uidA",
  "receiverId": "uidB",
  "text": "Hello",
  "createdAt": "serverTimestamp",
  "clientCreatedAt": "2026-06-12T10:15:00Z",
  "status": "sent"
}
```

Recommended `messageId`:

```text
yyyyMMddHHmmssfff_senderUid_random4
```

Example:

```text
20260612101530125_abcdUid_x7f2
```

This keeps messages sortable even before adding Firestore `orderBy`.

### User Chat Index

Path:

```text
users/{uid}/chatIndex/{chatId}
```

Data:

```json
{
  "chatId": "uidA_uidB",
  "otherUserId": "uidB",
  "otherDisplayName": "Ali",
  "lastMessage": "Hello",
  "lastMessageAt": "serverTimestamp",
  "unreadCount": 0
}
```

Why use a chat index?

- Fast sidebar loading.
- Avoid scanning all `chats`.
- Easier to show recent conversations.

## Send Message Flow

When player clicks another user:

1. Read local Firebase UID from the auth/session system.
2. Read other UID from `Photon.Realtime.Player.NickName`.
3. Build `chatId` using sorted UIDs.
4. Ensure `chats/{chatId}` exists.
5. Write message to `chats/{chatId}/messages/{messageId}`.
6. Update `chats/{chatId}` last message fields.
7. Update `users/{myUid}/chatIndex/{chatId}`.
8. Update `users/{otherUid}/chatIndex/{chatId}`.
9. Append message to local UI immediately as optimistic UI.

Important: Firestore batch writes are best for steps 5-8. Your current bridge does not expose batch writes, so first version can do separate writes. Later, add a JS bridge method for batch writes.

## Load Chat Flow

When opening a chat:

1. Build `chatId`.
2. Load `chats/{chatId}`.
3. Verify current UID matches `participantA` or `participantB` in the Stage 1 implementation.
4. Load `chats/{chatId}/messages`.
5. Sort messages by `clientCreatedAt` or message ID in C#.
6. Render messages in the side chat panel.

Current bridge note:

- `Firebase_Firestore_GetCollection(path)` can load a collection.
- It currently does not support `orderBy`, `limit`, or realtime listeners.
- First version can load all messages or last N by naming and client-side filtering.
- Better version should add `GetCollectionOrdered(path, orderField, limit)`.

## Realtime Chat Flow

For production chat, use Firestore realtime listeners:

```js
db.collection("chats")
  .doc(chatId)
  .collection("messages")
  .orderBy("createdAt")
  .limitToLast(50)
  .onSnapshot(...)
```

Your current bridge does not have `onSnapshot`. Add this later:

```text
Firebase_Firestore_ListenCollection(path, orderByField, limit, listenerId)
Firebase_Firestore_StopListener(listenerId)
```

Unity callback:

```text
OnFirebaseCollectionChanged(json)
```

This avoids polling and makes messages appear instantly.

## Recommended Implementation Stages

### Stage 1: Offline-safe Manual Load

Build the basic feature without realtime listeners.

Needed:

- `ChatIdentity` helper.
- `FirestoreChatService`.
- `ChatPanelUI`.
- `ChatMessageData`.
- `ChatThreadData`.
- Firestore rules.

Behavior:

- Click player.
- Load conversation.
- Send message.
- Refresh messages manually after sending or opening.

This is easiest and safest.

### Stage 2: Ordered Collection Reads

Extend the WebGL bridge:

```text
Firebase_Firestore_GetCollectionOrdered(path, orderByField, descending, limit)
```

Use it for:

```text
chats/{chatId}/messages ordered by createdAt limit 50
users/{uid}/chatIndex ordered by lastMessageAt limit 30
```

### Stage 3: Realtime Listeners

Add Firestore `onSnapshot` in the bridge.

Use it for:

- Active open conversation.
- Conversation sidebar index.

Stop listeners when:

- Chat panel closes.
- Scene changes.
- User logs out.
- WebGL app loses auth state.

## UI Flow

Suggested UI behavior:

1. Player clicks another avatar or player name.
2. Small action menu opens:

```text
View Profile
Message
Invite
```

3. Clicking `Message` opens right-side chat panel.
4. Chat panel header shows:

```text
Display Name
Online / In Scene / Last seen
```

5. Message list loads from Firestore.
6. Text input sends on Enter or Send button.
7. New sent message appears immediately with `Sending...`.
8. Firestore success changes status to `Sent`.
9. Firestore failure shows retry icon.

## PUN and Firebase Responsibilities

Use PUN for:

- Online presence.
- Current room/scene.
- Finding nearby players.
- Getting the other player's active display name and UID.

Use Firestore for:

- Persistent messages.
- Chat history.
- Recent conversations.
- Unread count.
- Offline message loading.

Do not use PUN RPCs for persistent chat history. RPC chat disappears when players leave and is not secure/persistent.

## Security Rules Draft

Recommended rules shape:

The original long-term schema uses `participantMap`. The Stage 1 Unity implementation writes `participantA` and `participantB` instead because Unity `JsonUtility` does not serialize dictionaries cleanly. Use rules based on `participantA` / `participantB` for the current scripts, or add a custom JSON serializer later if you want `participantMap`.

```js
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {

    function signedIn() {
      return request.auth != null;
    }

    function isChatParticipant(chatId) {
      return signedIn()
        && exists(/databases/$(database)/documents/chats/$(chatId))
        && (
          get(/databases/$(database)/documents/chats/$(chatId)).data.participantA == request.auth.uid
          || get(/databases/$(database)/documents/chats/$(chatId)).data.participantB == request.auth.uid
        );
    }

    match /users/{userId} {
      allow read: if signedIn();
      allow write: if signedIn() && request.auth.uid == userId;

      match /chatIndex/{chatId} {
        allow read: if signedIn() && request.auth.uid == userId;
        allow write: if signedIn() && request.auth.uid == userId;
      }
    }

    match /chats/{chatId} {
      allow read: if isChatParticipant(chatId);

      allow create: if signedIn()
        && (
          request.resource.data.participantA == request.auth.uid
          || request.resource.data.participantB == request.auth.uid
        )
        && request.resource.data.participantA is string
        && request.resource.data.participantB is string;

      allow update: if isChatParticipant(chatId);

      match /messages/{messageId} {
        allow read: if isChatParticipant(chatId);

        allow create: if isChatParticipant(chatId)
          && request.resource.data.senderId == request.auth.uid
          && request.resource.data.text is string
          && request.resource.data.text.size() > 0
          && request.resource.data.text.size() <= 1000;
      }
    }
  }
}
```

Important limitation:

- If the client writes both users' `chatIndex` documents, rules for writing `users/{otherUid}/chatIndex/{chatId}` become tricky, because normally a user should only write their own user document.
- Best long-term fix is a Cloud Function that updates chat indexes for both participants.
- First simple version can skip `users/{otherUid}/chatIndex` and only update local user's index, or relax rules carefully.

## Best Long-term Architecture

Best production flow:

1. Client writes only:

```text
chats/{chatId}/messages/{messageId}
```

2. Cloud Function validates and updates:

```text
chats/{chatId}.lastMessage
users/{uidA}/chatIndex/{chatId}
users/{uidB}/chatIndex/{chatId}
```

Why:

- Less trust in client.
- Better unread count accuracy.
- No duplicated client-side index update bugs.
- Cleaner Firestore rules.

For now, because this project is WebGL client-driven, we can start without Cloud Functions and add it later.

## Data Classes

Suggested C# models:

```csharp
[Serializable]
public class ChatThreadData
{
    public string chatId;
    public string[] participants;
    public string lastMessage;
    public string lastSenderId;
    public string updatedAt;
}

[Serializable]
public class ChatMessageData
{
    public string senderId;
    public string receiverId;
    public string text;
    public string createdAt;
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
    public int unreadCount;
}
```

## Firestore Paths

Helper methods:

```csharp
string GetChatId(string uidA, string uidB)
{
    return string.CompareOrdinal(uidA, uidB) < 0
        ? uidA + "_" + uidB
        : uidB + "_" + uidA;
}

string GetChatPath(string chatId)
{
    return "chats/" + chatId;
}

string GetMessagesPath(string chatId)
{
    return "chats/" + chatId + "/messages";
}

string GetMessagePath(string chatId, string messageId)
{
    return "chats/" + chatId + "/messages/" + messageId;
}
```

## Integration Points

### PUN_NetworkManager

Set nickname after Firebase login:

```text
displayName$firebaseUid
```

Avoid setting only random names when authenticated. Random names are fine for editor/demo mode, but persistent chat needs Firebase UID.

### PUN_SyncPlayer

Expose player identity to UI:

```text
PhotonView.Owner.NickName -> parse displayName and firebaseUid
```

### Chat UI

Input:

- Local user ID.
- Other user ID.
- Other display name.

Actions:

- `OpenChatWithUser(otherUid, displayName)`
- `SendMessage(text)`
- `LoadMessages(chatId)`
- Later: `StartListening(chatId)`

## Recommended First Implementation

Build this first:

1. `ChatIdentityUtility.cs`
2. `FirestoreChatService.cs`
3. `ChatPanelUI.cs`
4. Add `GetCollection` support to `FirebaseManager`, because the bridge already has it but the manager does not expose it generically.
5. Create Firestore rules for `chats`.
6. Test two WebGL browser windows with different Firebase users.

Do not start with realtime listeners. Get the pathing, rules, message send, and message load correct first.

## Implemented Stage 1 API

The first code stage is implemented in:

```text
Assets/Modules/Firebase/Scripts/Chat/ChatIdentityUtility.cs
Assets/Modules/Firebase/Scripts/Chat/FirestoreChatService.cs
Assets/Plugins/WebGL/FirebaseBridge.jslib
```

Main functions:

```csharp
string punName = ChatIdentityUtility.BuildPunNickname(displayName, firebaseUid);
bool ok = ChatIdentityUtility.TryParsePunNickname(otherPunName, out string otherName, out string otherUid);

string chatId = FirestoreChatService.GetChatId(myUid, otherUid);

FirestoreChatService.CreateOrOpenChat(
    myUid,
    otherUid,
    myDisplayName,
    otherDisplayName,
    thread => Debug.Log("Chat ready: " + thread.chatId),
    error => Debug.LogError(error)
);

FirestoreChatService.LoadRecentMessages(
    myUid,
    otherUid,
    messages => Debug.Log("Loaded: " + messages.Length),
    error => Debug.LogError(error)
);

FirestoreChatService.SendMessage(
    myUid,
    otherUid,
    inputText,
    message => Debug.Log("Sent: " + message.id),
    error => Debug.LogError(error),
    myDisplayName,
    otherDisplayName
);

FirestoreChatService.LoadChatIndex(
    myUid,
    chats => Debug.Log("Recent chats: " + chats.Length),
    error => Debug.LogError(error)
);
```

Message loading uses:

```text
chats/{chatId}/messages ordered by clientCreatedAt desc limit 50
```

The service sorts those 50 messages back into oldest-to-newest order before returning them to your UI.

Shortcut functions are also available when you already have the other Photon player's nickname:

```csharp
FirestoreChatService.LoadRecentMessagesFromPunNickname(myUid, otherPunName, onLoaded, onError);
FirestoreChatService.SendMessageFromPunNickname(myUid, myDisplayName, otherPunName, text, onSent, onError);
```

Current Stage 1 notes:

- This is manual load/send, not realtime `onSnapshot` yet.
- Message text is trimmed and limited to 1000 characters.
- `LoadRecentMessages` is capped at 50 messages by default.
- `SendMessage` writes the message, chat summary, and chat index documents with separate Firestore writes because the current bridge does not expose Firestore batch writes yet.
- If your Firestore rules do not allow a user to write `users/{otherUid}/chatIndex/{chatId}`, call `SendMessage(..., updateReceiverIndex: false)` or add a Cloud Function later to update the receiver index securely.

## Risks and Decisions

### Risk: PUN nickname spoofing

If a client can set any nickname, a malicious user could pretend their nickname contains another UID.

Mitigation:

- Use Firebase Auth UID as local source of truth.
- For other players, PUN nickname is acceptable for UI convenience, but sensitive writes should still be protected by Firestore rules.
- Later, use Photon custom auth or server-side user verification if needed.

### Risk: No realtime listener

Manual loading means messages do not appear instantly unless refreshed.

Mitigation:

- Stage 1 refresh after sending and when opening.
- Stage 3 add Firestore `onSnapshot`.

### Risk: Firestore costs

Realtime listeners and repeated loads can cost reads.

Mitigation:

- Limit messages to last 50.
- Use chat index for sidebar.
- Stop listeners when chat closes.

### Risk: Client-side lastMessage updates

Two users sending at same time can race.

Mitigation:

- Accept for first version.
- Later move last message/index updates to Cloud Functions.

## Final Recommendation

Use Firestore as the source of truth for chat history and PUN only as the way to discover currently visible users and their Firebase UID.

Start with deterministic direct-message chats:

```text
chats/{sortedUidA_sortedUidB}/messages/{messageId}
```

Then add:

- ordered queries,
- realtime listeners,
- chat sidebar index,
- Cloud Function cleanup/indexing.

This matches the current project architecture and avoids using Photon RPCs for persistent data.
