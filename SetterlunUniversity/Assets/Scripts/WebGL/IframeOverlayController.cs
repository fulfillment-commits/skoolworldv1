using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace SetterlunUniversity.WebGL
{
    /// <summary>
    /// Drives a browser-DOM iframe that overlays a UI Image inside a Unity Canvas.
    /// The Unity Image is treated purely as a layout placeholder — the real video /
    /// webpage lives in an HTML iframe layered above the WebGL canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public class IframeOverlayController : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector
        // ---------------------------------------------------------------
        [Header("Identity")]
        [Tooltip("Unique id so multiple overlays can coexist on one page.")]
        public string iframeId = "iframeOverlay";

        [Tooltip("Initial iframe URL. Vimeo / Loom share URLs are auto-converted to embed URLs.")]
        public string iframeUrl = "https://player.vimeo.com/video/1075842787";

        [Header("Layout Reference")]
        [Tooltip("The UI Image RectTransform that the iframe should match.")]
        public RectTransform targetRectTransform;

        [Tooltip("The Canvas that hosts the target RectTransform.")]
        public Canvas targetCanvas;

        [Header("Behaviour")]
        public bool showOnStart = false;
        public bool hideUnityPlaceholderImageWhenOpen = true;
        public bool syncEveryFrameWhileVisible = true;
        [Tooltip("If true, send pixel rect + Unity screen size; otherwise percent rect (recommended).")]
        public bool usePixelRectInsteadOfPercent = false;

        [Header("DOM Styling")]
        public int zIndex = 9999;
        [Range(0f, 1f)] public float opacity = 1f;
        public bool pointerEvents = true;
        public bool showCloseButton = true;

        [Header("Iframe Permissions")]
        public bool allowAutoplay = true;
        public bool allowFullscreen = true;
        public bool allowPictureInPicture = true;

        [Header("Sandbox (off by default — Vimeo/Loom may break if on)")]
        public bool useSandbox = false;
        public string sandboxPermissions = "allow-scripts allow-same-origin allow-popups allow-forms allow-presentation";

        [Serializable] public class StringEvent : UnityEvent<string> { }

        [Header("Events")]
        [Tooltip("Fires whenever the iframe becomes visible. Payload is the normalized embed URL currently loaded.")]
        public StringEvent OnIframeShown;
        [Tooltip("Fires whenever the iframe is hidden or destroyed.")]
        public UnityEvent OnIframeHidden;
        [Tooltip("Fires when the video inside the iframe reaches the end (YouTube / Vimeo only).")]
        public UnityEvent OnVideoEnded;

        /// <summary>The normalized embed URL currently loaded in the iframe (empty if none).</summary>
        public string CurrentUrl { get; private set; } = string.Empty;

        // ---------------------------------------------------------------
        // Internals
        // ---------------------------------------------------------------
        private Image _placeholderImage;
        private bool _created;
        private bool _visible;
        private string _lastSentUrl;

        // ---------------------------------------------------------------
        // jslib imports
        // ---------------------------------------------------------------
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void UIO_Create(
            string id, string url, int zIndex, float opacity, bool pointerEvents,
            bool showCloseBtn, bool allowAutoplay, bool allowFullscreen,
            bool allowPip, bool useSandbox, string sandboxPerms);

        [DllImport("__Internal")] private static extern void UIO_SetRectPercent(string id, float xPct, float yPct, float wPct, float hPct);
        [DllImport("__Internal")] private static extern void UIO_SetRectPixels(string id, float x, float y, float w, float h, float unityW, float unityH);
        [DllImport("__Internal")] private static extern void UIO_Show(string id);
        [DllImport("__Internal")] private static extern void UIO_Hide(string id);
        [DllImport("__Internal")] private static extern void UIO_Destroy(string id);
        [DllImport("__Internal")] private static extern void UIO_SetUrl(string id, string url);
        [DllImport("__Internal")] private static extern void UIO_SetZIndex(string id, int z);
        [DllImport("__Internal")] private static extern void UIO_SetOpacity(string id, float o);
        [DllImport("__Internal")] private static extern void UIO_SetPointerEvents(string id, bool enabled);
        [DllImport("__Internal")] private static extern int UIO_Exists(string id);
        [DllImport("__Internal")] private static extern int UIO_IsVisible(string id);
        [DllImport("__Internal")] private static extern void UIO_SetVideoEndCallback(string id, string goName, string methodName);
#endif

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        private void Awake()
        {
            if (targetRectTransform != null)
            {
                _placeholderImage = targetRectTransform.GetComponent<Image>();
            }
            if (targetCanvas == null && targetRectTransform != null)
            {
                targetCanvas = targetRectTransform.GetComponentInParent<Canvas>();
            }
        }

        private void Start()
        {
            if (showOnStart)
            {
                OpenIframe();
            }
        }

        private void LateUpdate()
        {
            if (_visible && syncEveryFrameWhileVisible)
            {
                SyncRectToDom();
            }
        }

        private void OnDisable()
        {
            // Hide (don't destroy) so re-enabling the component restores the overlay quickly.
            if (_visible)
            {
                HideIframe();
            }
        }

        private void OnDestroy()
        {
            DestroyIframe();
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------
        public void OpenIframe()
        {
            OpenIframeInternal(iframeUrl);
        }

        /// <summary>Open the overlay with a specific URL. Updates iframeUrl and fires OnIframeShown.</summary>
        public void OpenIframe(string url)
        {
            if (!string.IsNullOrEmpty(url)) iframeUrl = url;
            OpenIframeInternal(iframeUrl);
        }

        private void OpenIframeInternal(string rawUrl)
        {
            if (string.IsNullOrEmpty(iframeId))
            {
                Debug.LogError("[IframeOverlayController] iframeId is empty. Set a unique id.");
                return;
            }
            if (targetRectTransform == null)
            {
                Debug.LogError("[IframeOverlayController] targetRectTransform is not assigned.");
                return;
            }
            if (targetCanvas == null)
            {
                targetCanvas = targetRectTransform.GetComponentInParent<Canvas>();
                if (targetCanvas == null)
                {
                    Debug.LogError("[IframeOverlayController] targetCanvas is not assigned and could not be inferred.");
                    return;
                }
            }

            string url = NormalizeEmbedUrl(rawUrl);
            CurrentUrl = url ?? string.Empty;

#if UNITY_WEBGL && !UNITY_EDITOR
            if (!_created || UIO_Exists(iframeId) == 0)
            {
                UIO_Create(iframeId, url, zIndex, opacity, pointerEvents,
                    showCloseButton, allowAutoplay, allowFullscreen, allowPictureInPicture,
                    useSandbox, sandboxPermissions ?? string.Empty);
                UIO_SetVideoEndCallback(iframeId, gameObject.name, "OnVideoEndedFromJS");
                _created = true;
                _lastSentUrl = url;
            }
            else if (!string.Equals(url, _lastSentUrl, StringComparison.Ordinal))
            {
                UIO_SetUrl(iframeId, url);
                _lastSentUrl = url;
            }
            SyncRectToDom();
            UIO_Show(iframeId);
#else
            Debug.Log($"[IframeOverlayController] (Editor stub) OpenIframe id='{iframeId}' url='{url}'. " +
                      "Iframe will only render in a WebGL browser build.");
#endif

            _visible = true;
            ApplyPlaceholderVisibility();
            OnIframeShown?.Invoke(CurrentUrl);
        }

        public void ShowIframe()
        {
            if (!_created)
            {
                OpenIframeInternal(iframeUrl);
                return;
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            SyncRectToDom();
            UIO_Show(iframeId);
#else
            Debug.Log($"[IframeOverlayController] (Editor stub) ShowIframe id='{iframeId}' url='{CurrentUrl}'.");
#endif
            _visible = true;
            ApplyPlaceholderVisibility();
            OnIframeShown?.Invoke(CurrentUrl);
        }

        /// <summary>Show the overlay using a specific URL. Equivalent to SetUrlAndOpen.</summary>
        public void ShowIframe(string url)
        {
            if (!string.IsNullOrEmpty(url)) iframeUrl = url;
            OpenIframeInternal(iframeUrl);
        }

        public void HideIframe()
        {
            bool wasVisible = _visible;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_created) UIO_Hide(iframeId);
#else
            Debug.Log($"[IframeOverlayController] (Editor stub) HideIframe id='{iframeId}'.");
#endif
            _visible = false;
            ApplyPlaceholderVisibility();
            if (wasVisible) OnIframeHidden?.Invoke();
        }

        public void DestroyIframe()
        {
            bool wasVisible = _visible;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_created) UIO_Destroy(iframeId);
#endif
            _created = false;
            _visible = false;
            _lastSentUrl = null;
            CurrentUrl = string.Empty;
            ApplyPlaceholderVisibility();
            if (wasVisible) OnIframeHidden?.Invoke();
        }

        public void ToggleIframe()
        {
            if (_visible) HideIframe();
            else if (_created) ShowIframe();
            else OpenIframe();
        }

        public void SetUrl(string newUrl)
        {
            iframeUrl = newUrl;
            string normalized = NormalizeEmbedUrl(newUrl);
            CurrentUrl = normalized ?? string.Empty;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_created)
            {
                UIO_SetUrl(iframeId, normalized);
                _lastSentUrl = normalized;
            }
#else
            Debug.Log($"[IframeOverlayController] (Editor stub) SetUrl id='{iframeId}' url='{normalized}'.");
#endif
        }

        public void SetUrlAndOpen(string newUrl)
        {
            iframeUrl = newUrl;
            OpenIframeInternal(iframeUrl);
        }

        public void RefreshIframePosition()
        {
            if (!_created) return;
            SyncRectToDom();
        }

        /// <summary>Called by SendMessage from jslib when the video reaches the end.</summary>
        public void OnVideoEndedFromJS(string id)
        {
            if (id != iframeId) return;
            OnVideoEnded?.Invoke();
        }

        // ---------------------------------------------------------------
        // Position calculation
        // ---------------------------------------------------------------
        private void SyncRectToDom()
        {
            if (targetRectTransform == null || targetCanvas == null)
            {
                return;
            }

            Vector3[] worldCorners = new Vector3[4];
            targetRectTransform.GetWorldCorners(worldCorners);
            // 0 = bottom-left, 1 = top-left, 2 = top-right, 3 = bottom-right

            Camera cam = ResolveCanvasCamera(targetCanvas);

            Vector2 bl = WorldToScreen(cam, worldCorners[0]);
            Vector2 tr = WorldToScreen(cam, worldCorners[2]);

            float minX = Mathf.Min(bl.x, tr.x);
            float maxX = Mathf.Max(bl.x, tr.x);
            float minY = Mathf.Min(bl.y, tr.y);
            float maxY = Mathf.Max(bl.y, tr.y);

            float screenW = Screen.width;
            float screenH = Screen.height;
            if (screenW <= 0f || screenH <= 0f) return;

            float rectW = maxX - minX;
            float rectH = maxY - minY;

#if UNITY_WEBGL && !UNITY_EDITOR
            if (usePixelRectInsteadOfPercent)
            {
                // Convert Unity (bottom-left origin) to top-left origin pixels.
                float pxX = minX;
                float pxY = screenH - maxY;
                UIO_SetRectPixels(iframeId, pxX, pxY, rectW, rectH, screenW, screenH);
            }
            else
            {
                float xPct = (minX / screenW) * 100f;
                float yPct = ((screenH - maxY) / screenH) * 100f;
                float wPct = (rectW / screenW) * 100f;
                float hPct = (rectH / screenH) * 100f;
                UIO_SetRectPercent(iframeId, xPct, yPct, wPct, hPct);
            }
#else
            // Editor: nothing to push to. Provide a one-shot diagnostic so devs see numbers.
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"[IframeOverlayController] (Editor) rect for id='{iframeId}' " +
                          $"min=({minX:F1},{minY:F1}) size=({rectW:F1},{rectH:F1}) " +
                          $"screen=({screenW:F0},{screenH:F0})");
            }
#endif
        }

        private static Camera ResolveCanvasCamera(Canvas canvas)
        {
            if (canvas == null) return null;
            switch (canvas.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    // For overlay canvases, RectTransformUtility expects a null camera.
                    return null;
                case RenderMode.ScreenSpaceCamera:
                    return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                case RenderMode.WorldSpace:
                    return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                default:
                    return null;
            }
        }

        private static Vector2 WorldToScreen(Camera cam, Vector3 worldPoint)
        {
            // RectTransformUtility handles the overlay-vs-camera distinction correctly
            // (cam == null is the documented value for Screen Space Overlay canvases).
            return RectTransformUtility.WorldToScreenPoint(cam, worldPoint);
        }

        // ---------------------------------------------------------------
        // URL normalisation
        // ---------------------------------------------------------------
        private static readonly Regex LoomShareRegex =
            new Regex(@"^https?://(www\.)?loom\.com/share/([A-Za-z0-9]+)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex VimeoBareRegex =
            new Regex(@"^https?://(www\.)?vimeo\.com/(\d+)(/.*)?$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex VimeoEmbedRegex =
            new Regex(@"^https?://player\.vimeo\.com/video/",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex YouTubeWatchRegex =
            new Regex(@"^https?://(www\.)?youtube\.com/watch\?.*v=([A-Za-z0-9_\-]+)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex YouTubeShortRegex =
            new Regex(@"^https?://youtu\.be/([A-Za-z0-9_\-]+)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex YouTubeEmbedRegex =
            new Regex(@"^https?://(www\.)?youtube\.com/embed/",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string NormalizeEmbedUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;

            // Loom share → embed
            Match loomMatch = LoomShareRegex.Match(url);
            if (loomMatch.Success)
            {
                string id = loomMatch.Groups[2].Value;
                int q = url.IndexOf('?');
                string query = (q >= 0) ? url.Substring(q) : string.Empty;
                return $"https://www.loom.com/embed/{id}{query}";
            }

            // YouTube watch URL → embed with enablejsapi
            Match ytWatch = YouTubeWatchRegex.Match(url);
            if (ytWatch.Success)
                return $"https://www.youtube.com/embed/{ytWatch.Groups[2].Value}?enablejsapi=1";

            // YouTube short URL → embed with enablejsapi
            Match ytShort = YouTubeShortRegex.Match(url);
            if (ytShort.Success)
                return $"https://www.youtube.com/embed/{ytShort.Groups[1].Value}?enablejsapi=1";

            // YouTube already-embed URL → ensure enablejsapi=1
            if (YouTubeEmbedRegex.IsMatch(url))
                return AppendParam(url, "enablejsapi", "1");

            // Vimeo bare URL → embed with api=1
            Match vimeoMatch = VimeoBareRegex.Match(url);
            if (vimeoMatch.Success)
                return $"https://player.vimeo.com/video/{vimeoMatch.Groups[2].Value}?api=1";

            // Vimeo already-embed URL → ensure api=1
            if (VimeoEmbedRegex.IsMatch(url))
                return AppendParam(url, "api", "1");

            return url;
        }

        private static string AppendParam(string url, string key, string value)
        {
            string param = $"{key}={value}";
            if (url.Contains(param)) return url;
            return url.Contains('?') ? $"{url}&{param}" : $"{url}?{param}";
        }

        // ---------------------------------------------------------------
        // Placeholder image management
        // ---------------------------------------------------------------
        private void ApplyPlaceholderVisibility()
        {
            if (!hideUnityPlaceholderImageWhenOpen) return;
            if (_placeholderImage == null && targetRectTransform != null)
            {
                _placeholderImage = targetRectTransform.GetComponent<Image>();
            }
            if (_placeholderImage == null) return;
            _placeholderImage.enabled = !_visible;
        }

        // ---------------------------------------------------------------
        // Inspector live-edit support
        // ---------------------------------------------------------------
        private void OnValidate()
        {
            if (opacity < 0f) opacity = 0f;
            if (opacity > 1f) opacity = 1f;
            if (string.IsNullOrWhiteSpace(iframeId)) iframeId = "iframeOverlay";

#if UNITY_WEBGL && !UNITY_EDITOR
            // Cannot push live edits in the editor; build-time only.
#endif
        }
    }
}
