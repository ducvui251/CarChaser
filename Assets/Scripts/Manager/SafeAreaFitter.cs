using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CarChaser
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("Optional child SafeArea RectTransform. If empty, the existing MainCanvas HUD is laid out automatically.")]
        [SerializeField] private RectTransform safeArea;

        [Tooltip("Left, bottom, right, top padding in Canvas units. This is also the Web fallback because WebGL cannot report browser safe-area insets.")]
        [SerializeField] private Vector4 extraPadding = new Vector4(16f, 16f, 16f, 16f);

        [SerializeField] private float headerHeight = 128f;
        [SerializeField] private float bottomBarHeight = 120f;
        [SerializeField] private float edgeMargin = 16f;

        [Header("Hearts Layout Settings")]
        [SerializeField] private float heartsHeight = 60f;
        [SerializeField] private float heartsWidth = 200f;

        [Header("Score Layout Settings")]
        [Tooltip("If true, the auto-fit script overrides the Score object's position at runtime to place it directly under the TIME label. If false, the authored position and layout from the prefab/scene are preserved.")]
        [SerializeField] private bool overrideScorePosition = true;
        [SerializeField] private Vector2 scoreSize = new Vector2(160f, 50f);
        [SerializeField] private Vector2 scoreOffset = Vector2.zero;

        [Header("Preserved Layouts")]
        [Tooltip("Names of objects whose RectTransform in the prefab/scene is the source of truth. Their authored anchors, pivot, size, and position are preserved at runtime.")]
        [SerializeField] private string[] preserveAuthoredNames = new string[0];

        [Tooltip("Whether preserved objects should be offset by safe-area insets at runtime. When false, the exact authored position is maintained.")]
        [SerializeField] private bool offsetPreservedBySafeArea = false;

        private struct AuthoredLayout
        {
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
            public Vector2 offsetMin;
            public Vector2 offsetMax;
        }

        private Canvas canvas;
        private RectTransform canvasRect;
        private int lastWidth = -1;
        private int lastHeight = -1;
        private Rect lastSafeArea;
        private float lastScaleFactor = -1f;

        private readonly HashSet<RectTransform> preserved = new HashSet<RectTransform>();
        private readonly Dictionary<RectTransform, AuthoredLayout> authored = new Dictionary<RectTransform, AuthoredLayout>();
        private bool captured;

        private void Awake()
        {
            CacheComponents();
            CapturePreserved();
        }

        private void OnEnable()
        {
            lastWidth = -1;
            lastHeight = -1;
            lastSafeArea = Rect.zero;
            lastScaleFactor = -1f;
            CacheComponents();
            CapturePreserved();
        }

        [ContextMenu("Apply Layout Now")]
        public void ForceApply()
        {
            lastWidth = -1;
            lastHeight = -1;
            lastSafeArea = Rect.zero;
            lastScaleFactor = -1f;
            CacheComponents();
            ApplyIfNeeded();
        }

        private void LateUpdate()
        {
            CacheComponents();
            ApplyIfNeeded();
        }

        private void CacheComponents()
        {
            if (canvas == null)
                canvas = GetComponent<Canvas>();

            if (canvasRect == null)
                canvasRect = transform as RectTransform;
        }

        [ContextMenu("Capture Authored Layout")]
        public void CapturePreserved()
        {
            preserved.Clear();
            authored.Clear();

            if (preserveAuthoredNames != null)
            {
                for (int i = 0; i < preserveAuthoredNames.Length; i++)
                {
                    if (string.IsNullOrEmpty(preserveAuthoredNames[i]))
                        continue;

                    RectTransform rect = FindRect(preserveAuthoredNames[i]);
                    if (rect == null || !preserved.Add(rect))
                        continue;

                    RecordAuthored(rect);
                }
            }

            if (!overrideScorePosition)
            {
                RectTransform scoreRect = FindRect("Score");
                if (scoreRect != null && preserved.Add(scoreRect))
                {
                    RecordAuthored(scoreRect);
                }
            }

            captured = true;
        }

        private void RecordAuthored(RectTransform rect)
        {
            AuthoredLayout layout = new AuthoredLayout
            {
                anchorMin = rect.anchorMin,
                anchorMax = rect.anchorMax,
                pivot = rect.pivot,
                anchoredPosition = rect.anchoredPosition,
                sizeDelta = rect.sizeDelta,
                offsetMin = rect.offsetMin,
                offsetMax = rect.offsetMax
            };
            authored[rect] = layout;
        }

        private void ApplyIfNeeded()
        {
            if (canvas == null || canvasRect == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            if (!captured)
                CapturePreserved();

            Rect currentSafeArea = Screen.safeArea;
            if (currentSafeArea.width <= 0f || currentSafeArea.height <= 0f)
                currentSafeArea = new Rect(0f, 0f, Screen.width, Screen.height);

            float scaleFactor = Mathf.Max(0.0001f, canvas.scaleFactor);
            bool changed = lastWidth != Screen.width || lastHeight != Screen.height ||
                           lastSafeArea != currentSafeArea || !Mathf.Approximately(lastScaleFactor, scaleFactor);
            if (!changed)
                return;

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSafeArea = currentSafeArea;
            lastScaleFactor = scaleFactor;

            RectTransform nestedSafeArea = safeArea != null ? safeArea : FindRect("SafeArea");
            if (nestedSafeArea != null && nestedSafeArea != canvasRect)
            {
                ApplyNestedSafeArea(nestedSafeArea, currentSafeArea, scaleFactor);
                ApplyPreserved(currentSafeArea, scaleFactor);
            }
            else
            {
                ApplyExistingCanvasHud(currentSafeArea, scaleFactor);
                ApplyPreserved(currentSafeArea, scaleFactor);
            }
        }

        /// <summary>
        /// Re-asserts the authored RectTransform of every preserved object and optionally adds
        /// device safe-area insets, so the value shown in the editor is the value that renders.
        /// </summary>
        private void ApplyPreserved(Rect screenSafeArea, float scaleFactor)
        {
            if (preserved.Count == 0)
                return;

            float insetLeft = offsetPreservedBySafeArea ? screenSafeArea.xMin / scaleFactor : 0f;
            float insetBottom = offsetPreservedBySafeArea ? screenSafeArea.yMin / scaleFactor : 0f;
            float insetRight = offsetPreservedBySafeArea ? (Screen.width - screenSafeArea.xMax) / scaleFactor : 0f;
            float insetTop = offsetPreservedBySafeArea ? (Screen.height - screenSafeArea.yMax) / scaleFactor : 0f;

            foreach (KeyValuePair<RectTransform, AuthoredLayout> pair in authored)
            {
                RectTransform rect = pair.Key;
                if (rect == null)
                    continue;

                AuthoredLayout layout = pair.Value;

                // Never let another layout pass keep a rewritten value: start from the authored state.
                rect.anchorMin = layout.anchorMin;
                rect.anchorMax = layout.anchorMax;
                rect.pivot = layout.pivot;
                rect.sizeDelta = layout.sizeDelta;

                bool stretchX = !Mathf.Approximately(layout.anchorMin.x, layout.anchorMax.x);
                bool stretchY = !Mathf.Approximately(layout.anchorMin.y, layout.anchorMax.y);

                if (!stretchX && !stretchY)
                {
                    rect.anchoredPosition = layout.anchoredPosition;
                }
                else
                {
                    rect.offsetMin = layout.offsetMin;
                    rect.offsetMax = layout.offsetMax;
                }

                if (offsetPreservedBySafeArea)
                {
                    if (stretchX)
                    {
                        rect.offsetMin = new Vector2(layout.offsetMin.x + insetLeft, rect.offsetMin.y);
                        rect.offsetMax = new Vector2(layout.offsetMax.x - insetRight, rect.offsetMax.y);
                    }
                    else
                    {
                        float anchorX = layout.anchorMin.x;
                        float dx = insetLeft * (1f - anchorX) - insetRight * anchorX;
                        rect.anchoredPosition = new Vector2(layout.anchoredPosition.x + dx, rect.anchoredPosition.y);
                    }

                    if (stretchY)
                    {
                        rect.offsetMin = new Vector2(rect.offsetMin.x, layout.offsetMin.y + insetBottom);
                        rect.offsetMax = new Vector2(rect.offsetMax.x, layout.offsetMax.y - insetTop);
                    }
                    else
                    {
                        float anchorY = layout.anchorMin.y;
                        float dy = insetBottom * (1f - anchorY) - insetTop * anchorY;
                        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, layout.anchoredPosition.y + dy);
                    }
                }
            }
        }

        private bool IsPreserved(RectTransform rect)
        {
            if (rect == null)
                return false;

            if (rect.name == "Score")
                return !overrideScorePosition;

            return preserved.Contains(rect);
        }

        private void ApplyNestedSafeArea(RectTransform target, Rect screenSafeArea, float scaleFactor)
        {
            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Rect parentScreenRect = GetScreenRect(target.parent as RectTransform, eventCamera);
            Rect paddedSafeArea = new Rect(
                screenSafeArea.xMin + extraPadding.x * scaleFactor,
                screenSafeArea.yMin + extraPadding.y * scaleFactor,
                screenSafeArea.width - (extraPadding.x + extraPadding.z) * scaleFactor,
                screenSafeArea.height - (extraPadding.y + extraPadding.w) * scaleFactor);

            paddedSafeArea = Intersect(paddedSafeArea, parentScreenRect);
            if (paddedSafeArea.width <= 0f || paddedSafeArea.height <= 0f)
                paddedSafeArea = parentScreenRect;

            RectTransform parent = target.parent as RectTransform;
            if (parent == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, paddedSafeArea.min, eventCamera, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, paddedSafeArea.max, eventCamera, out Vector2 max);

            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.offsetMin = min - parent.rect.min;
            target.offsetMax = max - parent.rect.max;
        }

        private void ApplyExistingCanvasHud(Rect screenSafeArea, float scaleFactor)
        {
            float left = screenSafeArea.xMin / scaleFactor + extraPadding.x;
            float bottom = screenSafeArea.yMin / scaleFactor + extraPadding.y;
            float right = (Screen.width - screenSafeArea.xMax) / scaleFactor + extraPadding.z;
            float top = (Screen.height - screenSafeArea.yMax) / scaleFactor + extraPadding.w;

            RectTransform header = FindRect("Header");
            if (header != null && !IsPreserved(header))
                SetTopStretch(header, left, right, top, headerHeight);

            float labelTop = 12f;
            float labelHeight = 48f;
            float labelWidth = 100f;

            RectTransform bestLabel = FindRect("BestLabel");
            if (bestLabel != null && !IsPreserved(bestLabel) && header != null && bestLabel.parent == header)
            {
                Text bestLabelText = bestLabel.GetComponent<Text>();
                if (bestLabelText != null)
                {
                    float pref = bestLabelText.preferredWidth;
                    if (pref > 10f)
                        labelWidth = Mathf.Max(labelWidth, pref + 8f);

                    if (bestLabelText.alignment != TextAnchor.MiddleCenter)
                        bestLabelText.alignment = TextAnchor.MiddleCenter;
                }

                SetTopLeft(bestLabel, edgeMargin, labelTop, new Vector2(labelWidth, labelHeight));
            }

            RectTransform score = FindRect("Score");
            float labelCenterX = left + edgeMargin + (labelWidth * 0.5f) + scoreOffset.x;
            float scoreY = top + labelTop + labelHeight + 4f + scoreOffset.y;
            if (score != null && !IsPreserved(score) && score.parent == canvasRect)
            {
                // Position Score directly under the TIME label (BestLabel) and horizontally in its middle.
                score.anchorMin = new Vector2(0f, 1f);
                score.anchorMax = new Vector2(0f, 1f);
                score.pivot = new Vector2(0.5f, 1f);
                score.anchoredPosition = new Vector2(labelCenterX, -scoreY);
                score.sizeDelta = scoreSize;

                Text scoreText = score.GetComponent<Text>();
                if (scoreText != null)
                {
                    if (scoreText.alignment != TextAnchor.MiddleCenter)
                        scoreText.alignment = TextAnchor.MiddleCenter;
                    scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
                }
            }

            RectTransform bestScore = FindRect("BestScore");
            if (bestScore != null && !IsPreserved(bestScore) && header != null && bestScore.parent == header)
                SetTopLeft(bestScore, edgeMargin + labelWidth + 16f, labelTop, new Vector2(250f, 48f));

            float coinCenterX = edgeMargin + (labelWidth * 0.5f);
            float coinRowY = labelTop + (labelHeight * 0.5f);
            float coinTextTop = labelTop + labelHeight + 4f + scoreOffset.y;

            RectTransform coinImage = FindRect("CoinImg");
            if (coinImage != null && !IsPreserved(coinImage) && header != null && coinImage.parent == header)
            {
                coinImage.anchorMin = new Vector2(1f, 1f);
                coinImage.anchorMax = new Vector2(1f, 1f);
                coinImage.pivot = new Vector2(0.5f, 0.5f);
                coinImage.anchoredPosition = new Vector2(-coinCenterX, -coinRowY);
                coinImage.sizeDelta = new Vector2(40f, 40f);
            }

            RectTransform coinText = FindRect("CoinText");
            if (coinText != null && !IsPreserved(coinText) && header != null && coinText.parent == header)
            {
                coinText.anchorMin = new Vector2(1f, 1f);
                coinText.anchorMax = new Vector2(1f, 1f);
                coinText.pivot = new Vector2(0.5f, 1f);
                coinText.anchoredPosition = new Vector2(-coinCenterX, -coinTextTop);
                coinText.sizeDelta = new Vector2(160f, 50f);

                Text coinTextComp = coinText.GetComponent<Text>();
                if (coinTextComp != null)
                {
                    if (coinTextComp.alignment != TextAnchor.MiddleCenter)
                        coinTextComp.alignment = TextAnchor.MiddleCenter;
                    coinTextComp.horizontalOverflow = HorizontalWrapMode.Overflow;
                }
            }

            RectTransform hearts = FindRect("Hearts");
            if (hearts != null && !IsPreserved(hearts) && hearts.parent == canvasRect)
            {
                hearts.anchorMin = new Vector2(0f, 1f);
                hearts.anchorMax = new Vector2(0f, 1f);
                hearts.pivot = new Vector2(0.5f, 0.5f);

                HorizontalLayoutGroup heartsLayout = hearts.GetComponent<HorizontalLayoutGroup>();
                if (heartsLayout != null)
                    heartsLayout.childAlignment = TextAnchor.MiddleCenter;
                LayoutRebuilder.ForceRebuildLayoutImmediate(hearts);

                float canvasWidth = canvasRect.rect.width > 1f ? canvasRect.rect.width : Screen.width / scaleFactor;
                float safeWidth = Mathf.Max(0f, canvasWidth - left - right);
                float layoutWidth = LayoutUtility.GetPreferredWidth(hearts);
                if (layoutWidth <= 0f)
                    layoutWidth = hearts.rect.width;
                if (layoutWidth <= 0f)
                    layoutWidth = heartsWidth;

                float heartsX = left + safeWidth * 0.5f;
                float heartsY = top + coinRowY;
                hearts.anchoredPosition = new Vector2(heartsX, -heartsY);
                float h = Mathf.Min(heartsHeight, labelHeight + 4f + scoreSize.y);
                hearts.sizeDelta = new Vector2(layoutWidth, h);
                LayoutRebuilder.ForceRebuildLayoutImmediate(hearts);
            }

            float actionButtonY = bottom + edgeMargin + bottomBarHeight + 70f;
            RectTransform playButton = FindRect("PlayBtn");
            if (playButton != null && !IsPreserved(playButton) && playButton.parent == canvasRect)
                SetBottomCenter(playButton, actionButtonY, new Vector2(180f, 100f));

            RectTransform restartButton = FindRect("RestartBtn");
            if (restartButton != null && !IsPreserved(restartButton) && restartButton.parent == canvasRect)
                SetBottomCenter(restartButton, actionButtonY, new Vector2(180f, 100f));

            RectTransform dailyReward = FindRect("DailyRewardBtn");
            if (dailyReward != null && !IsPreserved(dailyReward) && dailyReward.parent == canvasRect)
            {
                dailyReward.anchorMin = new Vector2(1f, 0.5f);
                dailyReward.anchorMax = new Vector2(1f, 0.5f);
                dailyReward.pivot = new Vector2(0.5f, 0.5f);
                dailyReward.anchoredPosition = new Vector2(-right - 58f, (bottom - top) * 0.5f);
                dailyReward.sizeDelta = new Vector2(80f, 80f);
            }

            RectTransform menuButtons = FindRect("MenuButtons");
            if (menuButtons != null && !IsPreserved(menuButtons) && menuButtons.parent == canvasRect)
            {
                menuButtons.anchorMin = Vector2.zero;
                menuButtons.anchorMax = Vector2.right;
                menuButtons.pivot = new Vector2(0.5f, 0f);
                menuButtons.offsetMin = new Vector2(left + edgeMargin, bottom + edgeMargin);
                menuButtons.offsetMax = new Vector2(-right - edgeMargin, bottom + edgeMargin + bottomBarHeight);

                HorizontalLayoutGroup layout = menuButtons.GetComponent<HorizontalLayoutGroup>();
                if (layout != null)
                {
                    int activeChildren = 0;
                    for (int i = 0; i < menuButtons.childCount; i++)
                    {
                        if (menuButtons.GetChild(i).gameObject.activeSelf)
                            activeChildren++;
                    }

                    float availableWidth = Mathf.Max(0f, Screen.width / scaleFactor - left - right - edgeMargin * 2f);
                    float maximumSpacing = activeChildren > 1
                        ? (availableWidth - activeChildren * 72f) / (activeChildren - 1)
                        : 22f;
                    layout.spacing = Mathf.Clamp(maximumSpacing, 8f, 22f);
                }
            }

            RectTransform revival = FindRect("RevivalUI");
            if (revival != null && !IsPreserved(revival) && revival.parent == canvasRect)
            {
                revival.anchorMin = new Vector2(0.5f, 0.5f);
                revival.anchorMax = new Vector2(0.5f, 0.5f);
                revival.pivot = new Vector2(0.5f, 0.5f);
                revival.anchoredPosition = new Vector2((left - right) * 0.5f, (bottom - top) * 0.5f);
                float canvasWidth = canvasRect.rect.width > 1f ? canvasRect.rect.width : Screen.width / scaleFactor;
                float canvasHeight = canvasRect.rect.height > 1f ? canvasRect.rect.height : Screen.height / scaleFactor;
                revival.sizeDelta = new Vector2(
                    Mathf.Min(revival.sizeDelta.x, Mathf.Max(0f, canvasWidth - left - right - edgeMargin * 2f)),
                    Mathf.Min(revival.sizeDelta.y, Mathf.Max(0f, canvasHeight - top - bottom - edgeMargin * 2f)));
            }
        }

        private RectTransform FindRect(string objectName)
        {
            RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].name == objectName)
                    return rects[i];
            }

            return null;
        }

        private static void SetTopStretch(RectTransform rect, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTopLeft(RectTransform rect, float x, float top, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -top);
            rect.sizeDelta = size;
        }

        private static void SetTopRight(RectTransform rect, float right, float top, Vector2 size)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-right, -top);
            rect.sizeDelta = size;
        }

        private static void SetBottomCenter(RectTransform rect, float bottom, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            rect.sizeDelta = size;
        }

        private static Rect GetScreenRect(RectTransform rect, Camera eventCamera)
        {
            if (rect == null)
                return new Rect(0f, 0f, Screen.width, Screen.height);

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Rect Intersect(Rect first, Rect second)
        {
            float xMin = Mathf.Max(first.xMin, second.xMin);
            float yMin = Mathf.Max(first.yMin, second.yMin);
            float xMax = Mathf.Min(first.xMax, second.xMax);
            float yMax = Mathf.Min(first.yMax, second.yMax);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
