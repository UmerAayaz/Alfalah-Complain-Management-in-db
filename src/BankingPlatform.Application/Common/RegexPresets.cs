namespace BankingPlatform.Application.Common;

public sealed record RegexPreset(string Key, string Label, string Pattern, string Message);

public static class RegexPresets
{
    public static readonly IReadOnlyList<RegexPreset> All = new List<RegexPreset>
    {
        new("email", "Email",
            @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
            "Please enter a valid email address"),

        new("phone_pk", "Phone (Pakistan)",
            @"^(\+92|0)?3\d{9}$",
            "Enter a valid Pakistani phone (03001234567)"),

        new("cnic", "CNIC",
            @"^\d{5}-\d{7}-\d$",
            "Enter valid CNIC (42101-1234567-1)"),

        new("ntn", "NTN",
            @"^\d{7}-\d$",
            "Enter valid NTN (1234567-8)"),

        new("url", "URL",
            @"^https?:\/\/.+",
            "Enter a valid URL starting with http:// or https://"),

        new("postal_code", "Postal Code",
            @"^\d{5}$",
            "Enter a 5-digit postal code"),

        new("alpha_only", "Letters only",
            @"^[A-Za-z\s]+$",
            "Only letters and spaces allowed"),

        new("alphanumeric", "Letters and numbers",
            @"^[A-Za-z0-9]+$",
            "Only letters and numbers allowed"),

        new("digits_only", "Digits only",
            @"^\d+$",
            "Only digits allowed"),

        new("iban_pk", "IBAN (Pakistan)",
            @"^PK\d{2}[A-Z]{4}\d{16}$",
            "Enter valid IBAN (PK36SCBL...)"),
    };

    public static RegexPreset? Find(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? null
            : All.FirstOrDefault(p => p.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
}   