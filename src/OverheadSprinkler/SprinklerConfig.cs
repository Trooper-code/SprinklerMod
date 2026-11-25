using MelonLoader;

namespace OverheadSprinkler;

/// <summary>
/// MelonPreferences settings for Overhead Sprinkler (saved to UserData/MelonPreferences.cfg).
/// </summary>
public static class SprinklerConfig
{
    private static MelonPreferences_Category? _category;
    private static MelonPreferences_Entry<float>? _headPurchasePrice;
    private static MelonPreferences_Entry<float>? _sprayDurationSeconds;
    private static MelonPreferences_Entry<bool>? _enableSprayParticles;
    private static MelonPreferences_Entry<bool>? _enableSpraySound;
    private static MelonPreferences_Entry<float>? _soundVolume;

    public static float HeadPurchasePrice => _headPurchasePrice != null ? _headPurchasePrice.Value : 1500f;
    public static float SprayDurationSeconds => _sprayDurationSeconds != null ? _sprayDurationSeconds.Value : 4.5f;
    public static bool EnableSprayParticles => _enableSprayParticles == null || _enableSprayParticles.Value;
    public static bool EnableSpraySound => _enableSpraySound == null || _enableSpraySound.Value;
    public static float SoundVolume => _soundVolume != null ? _soundVolume.Value : 0.7f;

    public static void Initialize()
    {
        if (_category != null)
        {
            return;
        }

        _category = MelonPreferences.CreateCategory("OverheadSprinkler", "Overhead Sprinkler");

        _headPurchasePrice = _category.CreateEntry(
            "HeadPurchasePrice",
            1500f,
            "Head Purchase Price",
            "Purchase price of the Overhead Sprinkler Head at Oscar's Warehouse.");

        _sprayDurationSeconds = _category.CreateEntry(
            "SprayDurationSeconds",
            4.5f,
            "Spray Duration (Seconds)",
            "Duration in seconds of a watering cycle across 5 smooth segments (set 0 for instant).");

        _enableSprayParticles = _category.CreateEntry(
            "EnableSprayParticles",
            true,
            "Enable Spray Particles",
            "Show downward water/additive spray particles under each Overhead Sprinkler Head.");

        _enableSpraySound = _category.CreateEntry(
            "EnableSpraySound",
            true,
            "Enable Spray Sound",
            "Play vanilla sprinkler click and water spray sounds during activation.");

        _soundVolume = _category.CreateEntry(
            "SoundVolume",
            0.7f,
            "Sound Volume",
            "Volume multiplier (0.0 to 1.0) for sprinkler spray audio.");
    }
}
