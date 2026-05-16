using Content.Server.Access.Systems;
using Content.Shared._Misfits.RankTitle;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Utility;

namespace Content.Server._Misfits.RankTitle;

/// <summary>
/// When a <see cref="RankTitleComponent"/> item is worn in the neck slot:
/// — writes the rank title onto the wearer's ID card so the identity system
///   can display it in the "young private woman" description line.
/// — adds "She is wearing a private pin." to the character examine text.
/// </summary>
public sealed class RankTitleSystem : EntitySystem
{
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RankTitleComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<RankTitleComponent, GotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<InventoryComponent, ExaminedEvent>(OnExamined);
    }

    private void OnEquip(EntityUid uid, RankTitleComponent comp, GotEquippedEvent args)
        => SetRankTitle(args.Equipee, comp.RankTitle);

    private void OnUnequip(EntityUid uid, RankTitleComponent comp, GotUnequippedEvent args)
        => SetRankTitle(args.Equipee, null);

    private void SetRankTitle(EntityUid wearer, string? rankTitle)
    {
        if (!_idCard.TryFindIdCard(wearer, out var idCard))
            return;

        idCard.Comp.LocalizedJobTitle = rankTitle;
    }

    private void OnExamined(EntityUid uid, InventoryComponent _, ExaminedEvent args)
    {
        if (!_inventory.TryGetSlotEntity(uid, "neck", out var neckItem) || neckItem == null)
            return;

        if (!TryComp<RankTitleComponent>(neckItem.Value, out var rankComp) || rankComp == null)
            return;

        var itemName = FormattedMessage.EscapeText(Name(neckItem.Value));
        using (args.PushGroup(nameof(RankTitleComponent)))
        {
            args.PushMarkup(Loc.GetString("rank-pin-examine",
                ("user", Identity.Entity(uid, EntityManager)),
                ("item", itemName)));
        }
    }
}
