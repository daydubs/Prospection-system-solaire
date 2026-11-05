using System;
using System.Collections.Generic;
using InventoryFramework;

public sealed class ModuleRefundReceipt
{
    private readonly Dictionary<Item, int> _amounts = new Dictionary<Item, int>();
    public IEnumerable<KeyValuePair<Item, int>> Amounts => _amounts;
    public string Error { get; private set; }
    public string Note { get; private set; }

    public void Record(Item item, int amount)
    {
        if (item == null || amount <= 0) { Error = "Coût invalide"; return; }
        try { _amounts[item] = checked((_amounts.TryGetValue(item, out int n) ? n : 0) + amount); }
        catch (OverflowException) { Error = "Quantité de remboursement excessive"; }
    }

    public ModuleRefundReceipt Snapshot(BaseModuleData data, bool itemConsumed)
    {
        var copy = new ModuleRefundReceipt();
        foreach (var pair in _amounts) copy.Record(pair.Key, pair.Value);
        copy.Error = Error;
        var recipe = data != null ? data.DeconstructionRecipe : null;
        if (!itemConsumed) copy.Note = "Item non payé : impression non remboursée";
        else if (recipe == null) copy.Note = "Sans recette : seuls les coûts de pose sont rendus";
        else if (recipe.outputItem != data.Item || recipe.outputAmount <= 0 || recipe.ingredients == null)
            copy.Error = "Recette de déconstruction invalide";
        else
        {
            var totals = new ModuleRefundReceipt();
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient == null) { totals.Error = "Ingrédient invalide"; break; }
                totals.Record(ingredient.item, ingredient.amount);
            }
            if (totals.Error != null) copy.Error = totals.Error;
            foreach (var pair in totals._amounts)
            {
                if (pair.Value % recipe.outputAmount != 0) { copy.Error = "Recette multi-sortie non divisible : remboursement ambigu"; break; }
                copy.Record(pair.Key, pair.Value / recipe.outputAmount);
            }
            if (totals._amounts.Count == 0) copy.Note = "Recette sans ingrédients : coût d'impression nul";
        }
        return copy;
    }
}
