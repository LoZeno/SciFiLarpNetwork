using System.Net.Mail;

namespace SciFiLarpNetwork.Backend.Domain.Players;

/// <summary>
/// Rules for a player's optional email, phone, and notes. Blank values mean “not set”.
/// </summary>
public static class PlayerContacts
{
    public const int MaximumEmailLength = 254;
    public const int MaximumPhoneLength = 32;
    public const int MaximumNotesLength = 2000;

    public static bool IsValidEmail(string? email)
    {
        if (Normalize(email) is not { } trimmed)
        {
            return true;
        }

        return trimmed.Length <= MaximumEmailLength &&
               !trimmed.Any(char.IsWhiteSpace) &&
               MailAddress.TryCreate(trimmed, out var address) &&
               address.Address == trimmed;
    }

    public static bool IsValidPhone(string? phone) =>
        Normalize(phone) is not { } trimmed ||
        (trimmed.Length <= MaximumPhoneLength && !trimmed.Any(char.IsControl));

    public static bool IsValidNotes(string? notes) =>
        Normalize(notes) is not { } trimmed || trimmed.Length <= MaximumNotesLength;

    public static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
