using Comfort.Common;
using DebugTools.Utils;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GiveUIItem : MonoBehaviour
{
    [SerializeField] public GameObject Overlay;

    [SerializeField] public Image ItemImage;

    [SerializeField] public TMP_Text ItemText;

    [SerializeField] public Button SpawnButton;

    [SerializeField] public Button AddButton;

    public ItemIcon ItemIcon;
    private ItemData _data;

    private static readonly Vector2 _maxContainerSize = new(160f, 160f);

    private void Awake()
    {
        SetInteractable(false);
    }

    public void SetData(ItemData data, bool inRaid)
    {
        _data = data;

        ItemText.SetText(data.Name);
        var tooltip = ItemImage.gameObject.AddComponent<HoverTooltipArea>();
        tooltip.enabled = true;
        tooltip.SetMessageText(data.Description);

        if (inRaid)
        {
            SpawnButton.onClick.AddListener(SpawnItem);
            SpawnButton.gameObject.AddComponent<HoverTooltipArea>()
                .SetMessageText("Spawn in world");
            AddButton.onClick.AddListener(AddItem);
            AddButton.gameObject.AddComponent<HoverTooltipArea>()
                .SetMessageText("Spawn in inventory");
        }
    }

    public void SetInteractable(bool state)
    {
        SpawnButton.interactable = state;
        AddButton.interactable = state;
    }

    private void SpawnItem()
    {
        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
        DebugCommands.SpawnItem(_data.TemplateId, _data.TemplateInstance.StackMaxSize);
    }

    private void AddItem()
    {
        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
        DebugCommands.SpawnItemInInventory(_data.TemplateId, _data.TemplateInstance.StackMaxSize);
    }

    public void SetIcon(Sprite icon)
    {
        var rectTransform = ItemImage.rectTransform;

        ItemImage.sprite = icon;
        ItemImage.color = Color.white;

        ItemImage.SetNativeSize();

        var nativeWidth = rectTransform.rect.width;
        var nativeHeight = rectTransform.rect.height;

        if (nativeWidth <= 0f || nativeHeight <= 0f)
        {
            return;
        }

        var scaleX = _maxContainerSize.x / nativeWidth;
        var scaleY = _maxContainerSize.y / nativeHeight;

        var scaleFactor = (nativeWidth > _maxContainerSize.x || nativeHeight > _maxContainerSize.y)
            ? Mathf.Min(scaleX, scaleY)
            : 1f;

        rectTransform.sizeDelta = new Vector2(nativeWidth * scaleFactor, nativeHeight * scaleFactor);
    }

    public void DestroyOverlay()
    {
        if (Overlay != null)
        {
            GameObject.Destroy(Overlay);
        }
    }
}
