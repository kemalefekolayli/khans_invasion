/// <summary>
/// Validates carried-loot deposits. Kept separate so additional valid supply-city
/// policies can be added without changing General's wallet implementation.
/// </summary>
public static class CapitalLootDepositPolicy
{
    public static bool CanDeposit(General general)
    {
        return CanDeposit(general, out _);
    }

    public static bool CanDeposit(General general, out string reason)
    {
        reason = null;
        if (general == null)
        {
            reason = "No general selected";
            return false;
        }

        GeneralSelectionManager selectionManager = GeneralSelectionManager.Instance;
        SelectableGeneral selected = selectionManager != null ? selectionManager.SelectedGeneral : null;
        if (selected == null || selected.GetComponent<General>() != general)
        {
            reason = "Select the general carrying the loot";
            return false;
        }

        if (general.IsCaptured || (general.CommandedArmy != null && !general.CommandedArmy.IsPlayerArmy))
        {
            reason = "General is not player controlled";
            return false;
        }

        if (general.CarriedLoot <= 0f)
        {
            reason = "No carried loot";
            return false;
        }

        PlayerNation player = PlayerNation.Instance;
        if (player == null || player.currentNation == null || player.currentNation.capitalProvince == null)
        {
            reason = "Player capital is unavailable";
            return false;
        }

        CityCenter cityCenter = selected.CurrentCityCenter;
        ProvinceModel province = cityCenter != null ? cityCenter.Province : null;
        if (province == null
            || province != player.currentNation.capitalProvince
            || province.provinceOwner != player.currentNation)
        {
            reason = "Move to your capital to deposit loot";
            return false;
        }

        return true;
    }

    public static bool TryDeposit(General general, out float depositedAmount)
    {
        depositedAmount = 0f;
        if (!CanDeposit(general, out _)) return false;

        PlayerNation player = PlayerNation.Instance;
        depositedAmount = general.RemoveLoot(general.CarriedLoot);
        if (depositedAmount <= 0f) return false;

        player.nationMoney += depositedAmount;
        GameEvents.PlayerStatsChanged();
        GameLog.Log(GameLogCategory.Economy,
            $"[LootDeposit] {general.GeneralName} deposited {depositedAmount:F0} loot at the capital.");
        return true;
    }
}
