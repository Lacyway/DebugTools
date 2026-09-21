using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;

namespace DebugTools.Utils;

public sealed class ItemUIService
{
    private ItemData[] _itemDataCache;
    private readonly List<ItemData> _queryCache = new(_maxResults);

    private const int _maxResults = 50;

    public async Task Init()
    {
        var itemFactory = Singleton<ItemFactory>.Instance;
        var items = new List<ItemData>(itemFactory.ItemTemplates.Count);

        await WaitForLocales(itemFactory.ItemTemplates["55d355e64bdc2d962f8b4569"]);

        foreach ((var tpl, var item) in itemFactory.ItemTemplates)
        {
            if (item._type != NodeType.Item)
            {
                continue;
            }

            try
            {
                var localizedName = item.NameLocalizationKey.Localized();
                items.Add(new ItemData
                {
                    Name = localizedName,
                    NormalizedName = localizedName.ToLowerInvariant(),
                    Description = item.DescriptionLocalizationKey.Localized(),
                    TemplateId = tpl,
                    TemplateInstance = Singleton<ItemFactory>.Instance.GetPresetItem(tpl)
                });
            }
            catch (Exception ex)
            {
                DT_Plugin.DT_Logger.LogWarning($"Failed to create item {tpl}: {ex.Message}");
            }
        }

        _itemDataCache = [.. items];

        DT_Plugin.DT_Logger.LogInfo($"Cached {_itemDataCache.Length} items");
    }

    private async Task WaitForLocales(ItemTemplate itemTemplate)
    {
        var defaultValue = itemTemplate.NameLocalizationKey;
        while (defaultValue.Localized() == defaultValue)
        {
            await Task.Delay(TimeSpan.FromSeconds(1d));
        }
    }

    public List<ItemData> DoSearch(string query)
    {
        _queryCache.Clear();

        if (string.IsNullOrWhiteSpace(query) || _itemDataCache == null || _itemDataCache.Length == 0)
        {
            return _queryCache;
        }

        var querySpan = query.AsSpan();
        var normalizedQuerySpan = query.ToLowerInvariant().AsSpan();
        var checkTemplateId = query.Length >= 24; // standard MongoID length check to skip ID comparisons on short queries

        for (var i = 0; i < _itemDataCache.Length; i++)
        {
            ref readonly var item = ref _itemDataCache[i];
            if (item == null)
            {
                continue;
            }

            if (checkTemplateId && item.TemplateId.AsSpan()
                .Equals(querySpan, StringComparison.OrdinalIgnoreCase))
            {
                _queryCache.Add(item);
                return _queryCache;
            }

            if (item.NormalizedName.AsSpan()
                .Contains(normalizedQuerySpan, StringComparison.Ordinal))
            {
                _queryCache.Add(item);
                if (_queryCache.Count == _maxResults)
                {
                    break;
                }
            }
        }

        return _queryCache;
    }
}

public sealed record ItemData
{
    public string Name { get; set; }

    public string NormalizedName { get; set; }

    public string Description { get; set; }

    public string TemplateId { get; set; }

    public Item TemplateInstance { get; set; }
}
