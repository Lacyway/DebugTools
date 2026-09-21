using System.Collections.Generic;
using System.Threading.Tasks;
using Comfort.Common;
using DebugTools;
using DebugTools.Utils;
using EFT.InputSystem;
using EFT.UI;
using EFT.UI.DragAndDrop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GiveItemUI : InputNode
{
    [SerializeField] public Transform ItemView;

    [SerializeField] public RectTransform Root;

    [SerializeField] public RectTransform Header;

    [SerializeField] public GiveUIItem TemplateItem;

    [SerializeField] public TMP_InputField InputField;

    [SerializeField] public Button SearchButton;

    [SerializeField] public Button CloseButton;

    private readonly List<GiveUIItem> _results = [];
    private TextMeshProUGUI _buttonText;
    private bool _isSearching;

    private void Awake()
    {
        SearchButton.onClick.AddListener(async () => await DoSearch(InputField.text));
        InputField.onSubmit.AddListener(async (query) => await DoSearch(query));
        CloseButton.onClick.AddListener(Toggle);

        Header.gameObject.AddComponent<UIDragComponent>().Init(Root, true);
        gameObject.SetActive(false);
        TemplateItem.gameObject.SetActive(false); // sometimes I forget to do so in the editor :oldge:

        _buttonText = SearchButton.transform
            .GetChild(0)
            .GetComponent<TextMeshProUGUI>();

        DT_Plugin.InputTree.Add(this);

        DT_Plugin.DT_Logger.LogInfo("GiveItemUI ready");
    }

    private void OnEnable()
    {
        UIEventSystem.Instance.SetTemporaryStatus(true);
    }

    private void OnDisable()
    {
        UIEventSystem.Instance.SetTemporaryStatus(false);
    }

    private void Update()
    {
        if (!_isSearching && InputField != null && InputField.interactable)
        {
            var isControlOrCommand = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand);

            if (isControlOrCommand && Input.GetKeyDown(KeyCode.F))
            {
                FocusSearchField();
            }
        }
    }

    public void FocusSearchField()
    {
        if (InputField == null)
        {
            return;
        }

        InputField.ActivateInputField();
        InputField.Select();
    }

    public async Task DoSearch(string query)
    {
        if (_isSearching)
        {
            return;
        }

        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);

        _isSearching = true;
        SetInputState(false);
        _buttonText.SetText("...");

        try
        {
            ClearResults();

            var inRaid = DebugCommands.CheckForGame(true);

            foreach (var item in DT_Plugin.Instance.ItemUIService.DoSearch(query))
            {
                var newItem = GameObject.Instantiate(TemplateItem, ItemView);
                newItem.gameObject.SetActive(true);
                newItem.SetData(item, inRaid);
                if (item.TemplateInstance != null)
                {
                    var icon = await ItemViewFactory.GetItemSpriteAsync(item.TemplateInstance);
                    if (icon != null)
                    {
                        newItem.SetIcon(icon);
                    }
                }
                _results.Add(newItem);
            }

            foreach (var item in _results)
            {
                if (inRaid)
                {
                    item.SetInteractable(true);
                }
                item.DestroyOverlay();
            }

            DT_Plugin.DT_Logger.LogInfo($"Found {_results.Count} results");
        }
        finally
        {
            _isSearching = false;
            SetInputState(true);
            FocusSearchField();
            _buttonText.SetText("SEARCH");
        }
    }

    private void ClearResults()
    {
        foreach (var item in _results)
        {
            GameObject.Destroy(item.gameObject);
        }
        _results.Clear();
    }

    private void SetInputState(bool enabled)
    {
        if (SearchButton != null)
        {
            SearchButton.interactable = enabled;
        }

        if (InputField != null)
        {
            InputField.interactable = enabled;
        }

        if (CloseButton != null)
        {
            CloseButton.interactable = enabled;
        }
    }

    public void Toggle()
    {
        if (_isSearching)
        {
            return;
        }

        gameObject.SetActive(!gameObject.activeSelf);
        if (!gameObject.activeSelf)
        {
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuEscape);
        }
    }

    private void OnDestroy()
    {
        ClearResults();
    }

    public override ETranslateResult TranslateCommand(ECommand command)
    {
        if (command.IsCommand(ECommand.Escape))
        {
            Toggle();
            return ETranslateResult.BlockAll;
        }

        return GetDefaultBlockResult(command);
    }

    public override ECursorResult ShouldLockCursor()
    {
        return ECursorResult.ShowCursor;
    }

    public override void TranslateAxes(ref float[] axes)
    {
        axes = null;
    }
}
