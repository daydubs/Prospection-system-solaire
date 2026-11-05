using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryFramework
{
    public class ItemTooltip : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public Image icon;
        public RectTransform rectTransform;
        public Vector2 offset;

        [Header("Fixed Information Panel")]
        [SerializeField] private RectTransform _informationAnchor;
        [SerializeField] private float _panelHeight = 300f;
        [SerializeField] private float _panelGap = 18f;

        private Canvas rootCanvas;
        private bool _isShowing;

        void Awake()
        {
            rootCanvas = GetComponentInParent<Canvas>();
            GetComponent<CanvasGroup>().blocksRaycasts = false;
            if (!_isShowing) Hide();
        }

        public void Show(Item item, Vector2 screenPos)
        {
            if (item == null) return;

            _isShowing = true;
            nameText.text = item.itemName;
            descriptionText.text = item.description;
            icon.sprite = item.icon;

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            UpdatePosition(screenPos);
        }

        public void Hide()
        {
            _isShowing = false;
            gameObject.SetActive(false);
        }

        public void UpdatePosition(Vector2 screenPos)
        {
            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
            if (_informationAnchor != null)
            {
                RectTransform parentRect = rectTransform.parent as RectTransform;
                Canvas anchorCanvas = _informationAnchor.GetComponentInParent<Canvas>();
                Camera anchorCamera = anchorCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : anchorCanvas.worldCamera;
                Camera panelCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
                Vector2 leftBottom;
                Vector2 rightBottom;
                Vector3 leftWorld = _informationAnchor.TransformPoint(new Vector3(_informationAnchor.rect.xMin, _informationAnchor.rect.yMin));
                Vector3 rightWorld = _informationAnchor.TransformPoint(new Vector3(_informationAnchor.rect.xMax, _informationAnchor.rect.yMin));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, RectTransformUtility.WorldToScreenPoint(anchorCamera, leftWorld), panelCamera, out leftBottom);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, RectTransformUtility.WorldToScreenPoint(anchorCamera, rightWorld), panelCamera, out rightBottom);
                rectTransform.anchorMin = rectTransform.anchorMax = Vector2.one;
                rectTransform.pivot = Vector2.one;
                Vector2 panelTopRight = rightBottom + Vector2.down * _panelGap;
                rectTransform.anchoredPosition = panelTopRight - parentRect.rect.max;
                rectTransform.sizeDelta = new Vector2(rightBottom.x - leftBottom.x, Mathf.Min(_panelHeight, Mathf.Max(0f, panelTopRight.y - parentRect.rect.yMin - _panelGap)));
                return;
            }

            Vector2 localPoint;
            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootCanvas.transform as RectTransform, screenPos, cam, out localPoint);
            rectTransform.anchoredPosition = localPoint + offset;
        }
    }

}
