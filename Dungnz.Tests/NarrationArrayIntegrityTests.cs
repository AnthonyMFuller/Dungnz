using Dungnz.Data;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// #945 — Verifies all narration/message string arrays are non-empty, contain no null entries,
/// and have no duplicates within individual arrays.
/// </summary>
public class NarrationArrayIntegrityTests
{
    private static void AssertArrayValid(string[] array, string name)
    {
        array.Should().NotBeEmpty($"{name} should have at least one entry");
        array.Should().NotContainNulls($"{name} should not contain null entries");
        array.Should().OnlyHaveUniqueItems($"{name} should not contain duplicates");
    }

    // ── CombatNarration ──────────────────────────────────────────────────────

    [Fact] public void CombatNarration_StartGeneric_Valid() => AssertArrayValid(CombatNarration.StartGeneric, "StartGeneric");
    [Fact] public void CombatNarration_StartUndead_Valid() => AssertArrayValid(CombatNarration.StartUndead, "StartUndead");
    [Fact] public void CombatNarration_NearDeath_Valid() => AssertArrayValid(CombatNarration.NearDeath, "NearDeath");
    [Fact] public void CombatNarration_KillMelee_Valid() => AssertArrayValid(CombatNarration.KillMelee, "KillMelee");
    [Fact] public void CombatNarration_KillRanged_Valid() => AssertArrayValid(CombatNarration.KillRanged, "KillRanged");
    [Fact] public void CombatNarration_KillMagic_Valid() => AssertArrayValid(CombatNarration.KillMagic, "KillMagic");
    [Fact] public void CombatNarration_KillGeneric_Valid() => AssertArrayValid(CombatNarration.KillGeneric, "KillGeneric");
    [Fact] public void CombatNarration_CritFlavor_Valid() => AssertArrayValid(CombatNarration.CritFlavor, "CritFlavor");
    [Fact] public void CombatNarration_EnemySpecialAttack_Valid() => AssertArrayValid(CombatNarration.EnemySpecialAttack, "EnemySpecialAttack");

    // ── RoomStateNarration ───────────────────────────────────────────────────

    [Fact] public void RoomStateNarration_ClearedRoom_Valid() => AssertArrayValid(RoomStateNarration.ClearedRoom, "ClearedRoom");
    [Fact] public void RoomStateNarration_RevisitedRoom_Valid() => AssertArrayValid(RoomStateNarration.RevisitedRoom, "RevisitedRoom");

    // ── MerchantNarration ────────────────────────────────────────────────────

    [Fact] public void MerchantNarration_Greetings_Valid() => AssertArrayValid(MerchantNarration.Greetings, "Greetings");
    [Fact] public void MerchantNarration_AfterPurchase_Valid() => AssertArrayValid(MerchantNarration.AfterPurchase, "AfterPurchase");
    [Fact] public void MerchantNarration_CantAfford_Valid() => AssertArrayValid(MerchantNarration.CantAfford, "CantAfford");

    // ── AmbientEvents ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void AmbientEvents_ForFloor_Valid(int floor)
    {
        var lines = AmbientEvents.ForFloor(floor);
        lines.Should().NotBeEmpty($"floor {floor} should have ambient events");
        lines.Should().NotContainNulls();
    }
}
