namespace MaxButtonsRedux;

public static class MaxButtonCrafting
{
    public static void AddMinAndMaxButtons(CraftItemGUI craftItemGUI, string parentButtonName, string minMaxButtonName, bool isMaximum, WorldGameObject crafteryWgo)
    {
        if (!craftItemGUI.current_craft.CanCraftMultiple())
        {
            return;
        }

        // Primary install: the vanilla amount-button strip lives at "selection frame/amount buttons/".
        // Clone the cloned Min/Max as a sibling of the vanilla L/R there. Visible in normal
        // (collapsed) view; hidden when the row expands because CraftItemGUI.Redraw deactivates
        // selection_frame at line 261 for non-gamepad players.
        var legacyHost = craftItemGUI.transform.Find("selection frame/amount buttons");
        var sourceButton = legacyHost?.Find(parentButtonName);
        if (legacyHost == null || sourceButton == null)
        {
            return;
        }

        if (legacyHost.Find(minMaxButtonName) == null)
        {
            // First-time install: shrink the vanilla source button so the cloned Min/Max sits
            // cleanly below it. Idempotent — guarded by the legacy-host duplicate check above.
            sourceButton.localPosition = new Vector3(sourceButton.localPosition.x, -10f, sourceButton.localPosition.z);
            sourceButton.GetComponent<UI2DSprite>().SetDimensions(26, 26);
            sourceButton.GetComponent<BoxCollider2D>().size = new Vector2(29.4f, 26f);

            InstallClone(craftItemGUI, sourceButton.gameObject, legacyHost, minMaxButtonName, isMaximum, crafteryWgo,
                new Vector3(sourceButton.localPosition.x, -31f, sourceButton.localPosition.z));
        }

        // Secondary install: full_detailed_go has no vanilla amount-button siblings at all (the
        // expand view was never designed for amount controls), so we add the full set ourselves —
        // singular (+1/-1) on top, Min/Max on bottom, two per side. Each call to this method
        // handles one side: the R/max call adds +1 above Max on the right, the L/min call adds
        // -1 above Min on the left. Final positions per user spec — measured against the expand
        // panel's local space, with the bottom row (Min/Max) ~21 units below the top row.
        if (craftItemGUI.full_detailed_go != null)
        {
            var detailHost = craftItemGUI.full_detailed_go.transform;
            var sideX = isMaximum ? 109f : -281.7757f;
            const float topY = -10f;
            const float bottomY = -31.1383f;

            // Singular +1 (right) or -1 (left) on top — name matches the vanilla button name so
            // we don't dupe across re-installs. Wires to OnAmountPlus/OnAmountMinus directly.
            if (detailHost.Find(parentButtonName) == null)
            {
                InstallSingularClone(craftItemGUI, sourceButton.gameObject, detailHost, parentButtonName, isMaximum,
                    new Vector3(sideX, topY, 0f));
            }

            // Min/Max on bottom — uses the same SetMin/SetMax logic as the primary install.
            if (detailHost.Find(minMaxButtonName) == null)
            {
                InstallClone(craftItemGUI, sourceButton.gameObject, detailHost, minMaxButtonName, isMaximum, crafteryWgo,
                    new Vector3(sideX, bottomY, 0f));
            }
        }
    }

    private static void InstallClone(CraftItemGUI craftItemGUI, GameObject sourceGo, Transform host, string minMaxButtonName, bool isMaximum, WorldGameObject crafteryWgo, Vector3 localPos)
    {
        var sourceUi = sourceGo.GetComponent<UIButton>();

        var minMaxButton = Object.Instantiate(sourceGo, host);
        minMaxButton.name = minMaxButtonName;
        minMaxButton.transform.localPosition = localPos;

        var minMaxButtonUI = minMaxButton.GetComponent<UIButton>();
        minMaxButtonUI.normalSprite2D = sourceUi.normalSprite2D;
        minMaxButtonUI.hoverSprite2D = sourceUi.hoverSprite2D;
        minMaxButtonUI.pressedSprite2D = sourceUi.pressedSprite2D;
        minMaxButtonUI.onClick = [];

        if (isMaximum)
        {
            EventDelegate.Add(minMaxButtonUI.onClick, delegate { SetMaximumAmount(craftItemGUI, crafteryWgo); });
        }
        else
        {
            EventDelegate.Add(minMaxButtonUI.onClick, delegate { SetMinimumAmount(craftItemGUI); });
        }

        var arrowSpriteTransform = minMaxButton.transform.Find("arrow spr");
        if (arrowSpriteTransform == null)
        {
            return;
        }
        arrowSpriteTransform.name = "arrow spr 1";
        arrowSpriteTransform.localPosition += new Vector3(4f, 0f, 0f);

        CloneAndPositionSprite(arrowSpriteTransform, "arrow spr 2", -4f);
        CloneAndPositionSprite(arrowSpriteTransform, "arrow spr 3", -8f);
    }
    
    private static void CloneAndPositionSprite(Transform spriteTransform, string spriteName, float xOffset)
    {
        var clonedSprite = Object.Instantiate(spriteTransform.gameObject, spriteTransform.parent);
        clonedSprite.name = spriteName;
        clonedSprite.transform.localPosition += new Vector3(xOffset, 0f, 0f);
    }

    // Adds a singular +1/-1 button under full_detailed_go by cloning the legacy R/L source and
    // wiring its onClick to OnAmountPlus/OnAmountMinus. No extra arrow-sprite stack — this is a
    // one-step button, not a Min/Max stack.
    private static void InstallSingularClone(CraftItemGUI craftItemGUI, GameObject sourceGo, Transform host, string buttonName, bool isPlus, Vector3 localPos)
    {
        var sourceUi = sourceGo.GetComponent<UIButton>();

        var btn = Object.Instantiate(sourceGo, host);
        btn.name = buttonName;
        btn.transform.localPosition = localPos;

        var btnUi = btn.GetComponent<UIButton>();
        btnUi.normalSprite2D = sourceUi.normalSprite2D;
        btnUi.hoverSprite2D = sourceUi.hoverSprite2D;
        btnUi.pressedSprite2D = sourceUi.pressedSprite2D;
        btnUi.onClick = [];

        if (isPlus)
        {
            EventDelegate.Add(btnUi.onClick, craftItemGUI.OnAmountPlus);
        }
        else
        {
            EventDelegate.Add(btnUi.onClick, craftItemGUI.OnAmountMinus);
        }
    }


    internal static void SetMinimumAmount(CraftItemGUI craftItemGUI)
    {
        SetAmount(craftItemGUI, 1);
    }


    internal static void SetMaximumAmount(CraftItemGUI craftItemGUI, WorldGameObject crafteryWgo)
    {
        var maxCraftableFromWgo = 9999;
        var multiInventory = GlobalCraftControlGUI.is_global_control_active
            ? GUIElements.me.craft.multi_inventory
            : MainGame.me.player.GetMultiInventoryForInteraction(null);

        foreach (var neededItemFromWgo in craftItemGUI.craft_definition.needs_from_wgo)
        {
            if (neededItemFromWgo != null && crafteryWgo != null && crafteryWgo.data != null && neededItemFromWgo.id == "fire" && neededItemFromWgo.value > 0)
            {
                maxCraftableFromWgo = crafteryWgo.data.GetTotalCount(neededItemFromWgo.id, true) / neededItemFromWgo.value;
            }

            if (maxCraftableFromWgo > 1) continue;
            SetAmount(craftItemGUI, 1);
            return;
        }

        // autoSelectHighestQuality: true so the Max button reproduces QueueEverything's auto-max
        // semantic (lock _multiquality_ids to the highest available tier and compute that tier's
        // max). Without this, pressing Max after the user changes selection mid-window can return
        // a mixed-tier total that disagrees with what auto-max would have set on open.
        var info = CraftMaxCalculator.Calculate(craftItemGUI, multiInventory, autoSelectHighestQuality: true);
        if (info.NotCraftable.Count > 0 || info.Min <= 0)
        {
            SetAmount(craftItemGUI, 1);
            return;
        }

        var finalMaxCraftable = Math.Min(info.Min, maxCraftableFromWgo);
        finalMaxCraftable = Math.Max(finalMaxCraftable, 1);
        SetAmount(craftItemGUI, finalMaxCraftable);
    }


    private static void SetAmount(CraftItemGUI craftItemGUI, int amount)
    {
        craftItemGUI._amount = amount;
        craftItemGUI.Redraw();
        craftItemGUI.OnOver();
    }
}