using System.IO;
using System.Text.Json;
using Dungnz.Systems;
using FluentAssertions;

namespace Dungnz.Tests;

public class StatusEffectRegistryTests : IDisposable
{
    private readonly string _tempFile;

    public StatusEffectRegistryTests()
    {
        _tempFile = Path.GetTempFileName();
        StatusEffectRegistry.Clear();
    }

    public void Dispose() { File.Delete(_tempFile); StatusEffectRegistry.Clear(); }

    [Fact]
    public void Load_ParsesEffectsFromJson()
    {
        var defs = new[]
        {
            new { id = "Poison", name = "Poison", description = "Toxic", durationRounds = 3, tickDamage = (int?)3, statModifiers = (object?)null },
            new { id = "Burn", name = "Burning", description = "Fire", durationRounds = 2, tickDamage = (int?)8, statModifiers = (object?)null },
            new { id = "Freeze", name = "Chilled", description = "Ice", durationRounds = 2, tickDamage = (int?)null, statModifiers = (object?)null }
        };
        File.WriteAllText(_tempFile, JsonSerializer.Serialize(defs));

        StatusEffectRegistry.Load(_tempFile);

        StatusEffectRegistry.All.Should().HaveCount(3);
        StatusEffectRegistry.Get("Poison").Should().NotBeNull();
        StatusEffectRegistry.Get("Burn")!.TickDamage.Should().Be(8);
        StatusEffectRegistry.Get("Freeze")!.TickDamage.Should().BeNull();
    }

    [Fact]
    public void Get_ReturnsNull_ForUnknownId()
    {
        StatusEffectRegistry.Get("NonExistent").Should().BeNull();
    }

    [Fact]
    public void GetTickDamage_ReturnsFallback_WhenNotLoaded()
    {
        StatusEffectRegistry.GetTickDamage("Poison", 99).Should().Be(99);
    }

    [Fact]
    public void GetTickDamage_ReturnsDataValue_WhenLoaded()
    {
        var defs = new[] { new { id = "Poison", name = "Poison", description = "", durationRounds = 3, tickDamage = (int?)7, statModifiers = (object?)null } };
        File.WriteAllText(_tempFile, JsonSerializer.Serialize(defs));
        StatusEffectRegistry.Load(_tempFile);

        StatusEffectRegistry.GetTickDamage("Poison", 3).Should().Be(7);
    }

    [Fact]
    public void GetDuration_ReturnsFallback_WhenNotLoaded()
    {
        StatusEffectRegistry.GetDuration("Burn", 5).Should().Be(5);
    }

    [Fact]
    public void GetDuration_ReturnsDataValue_WhenLoaded()
    {
        var defs = new[] { new { id = "Burn", name = "Burning", description = "", durationRounds = 4, tickDamage = (int?)null, statModifiers = (object?)null } };
        File.WriteAllText(_tempFile, JsonSerializer.Serialize(defs));
        StatusEffectRegistry.Load(_tempFile);

        StatusEffectRegistry.GetDuration("Burn", 2).Should().Be(4);
    }

    [Fact]
    public void Load_IsCaseInsensitive()
    {
        var defs = new[] { new { id = "Poison", name = "Poison", description = "", durationRounds = 3, tickDamage = (int?)3, statModifiers = (object?)null } };
        File.WriteAllText(_tempFile, JsonSerializer.Serialize(defs));
        StatusEffectRegistry.Load(_tempFile);

        StatusEffectRegistry.Get("poison").Should().NotBeNull();
        StatusEffectRegistry.Get("POISON").Should().NotBeNull();
    }

    [Fact]
    public void StatusEffects_Json_Loads_Successfully()
    {
        StatusEffectRegistry.Load("Data/status-effects.json");

        StatusEffectRegistry.All.Should().HaveCountGreaterThanOrEqualTo(3);
        StatusEffectRegistry.Get("Poison").Should().NotBeNull();
        StatusEffectRegistry.Get("Burn").Should().NotBeNull();
        StatusEffectRegistry.Get("Freeze").Should().NotBeNull();
    }
}
