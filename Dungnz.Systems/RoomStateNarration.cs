namespace Dungnz.Systems;

/// <summary>
/// Flavor text pools for room state transitions: the grim aftermath of a cleared room,
/// and the uneasy familiarity of revisiting somewhere you've already been.
/// </summary>
public static class RoomStateNarration
{
    /// <summary>Shown briefly after the last enemy in a room is killed.</summary>
    public static readonly string[] ClearedRoom =
    {
        "The floor is darker now.",
        "Something drips. You don't look up.",
        "It's quiet again. Not peaceful — just quiet.",
        "The silence that follows a kill is its own kind of sound.",
        "You breathe. The room doesn't.",
        "Whatever this place was, it's a little more yours now.",
        "The air smells of copper and effort.",
        "Nothing left here but what you brought with you.",
        "Still. Everything holds very still.",
        "The fight is behind you. The dungeon isn't."
    };

    /// <summary>Shown when the player enters a room they've already explored.</summary>
    public static readonly string[] RevisitedRoom =
    {
        "You've been here. The smell hasn't improved.",
        "The marks of your passage are already fading.",
        "Same walls. Different you.",
        "Familiar in the way that bad memories are familiar.",
        "You know this room. It doesn't care.",
        "The quiet here is old — you didn't start it.",
        "Your footprints are already in the dust.",
        "Nothing has changed. Nothing ever does, down here.",
        "You've walked this floor before. The stones remember.",
        "Returning doesn't make it safer."
    };
}
