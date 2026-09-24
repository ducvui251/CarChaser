using UnityEngine;
using UnityEngine.UI;

namespace CarChaser
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class DynamicCanvasScaler : MonoBehaviour
    {
        [SerializeField] private float referenceAspect = 640f / 1136f;
        [SerializeField, Range(0f, 1f)] private float tallScreenMatch = 0f;
        [SerializeField, Range(0f, 1f)] private float wideScreenMatch = 1f;

        private CanvasScaler canvasScaler;
        private int lastWidth = -1;
        private int lastHeight = -1;
        private float lastMatch = -1f;

        private void Awake()
        {
            canvasScaler = GetComponent<CanvasScaler>();
            Apply(true);
        }

        private void OnEnable()
        {
            if (canvasScaler == null)
                canvasScaler = GetComponent<CanvasScaler>();

            Apply(true);
        }

        private void Update()
        {
            Apply(false);
        }

        private void Apply(bool force)
        {
            if (canvasScaler == null || canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                return;

            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            float aspect = (float)width / height;
            float match = aspect < referenceAspect ? tallScreenMatch : wideScreenMatch;

            if (!force && width == lastWidth && height == lastHeight && Mathf.Approximately(match, lastMatch))
                return;

            canvasScaler.matchWidthOrHeight = Mathf.Clamp01(match);
            lastWidth = width;
            lastHeight = height;
            lastMatch = canvasScaler.matchWidthOrHeight;
        }
    }
}
