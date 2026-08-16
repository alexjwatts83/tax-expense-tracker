namespace TaxExpenseTrackerDevLauncher.Models;

public sealed record PhoneViewport(string Name, int Width, int Height)
{
    public string DisplayName => $"{Name} ({Width} x {Height})";

    public static IReadOnlyList<PhoneViewport> KnownPhones { get; } =
    [
        new("Samsung Galaxy S24", 360, 780),
        new("Samsung Galaxy S24 Ultra", 412, 915),
        new("Samsung Galaxy A54", 384, 854),
        new("Google Pixel 8", 412, 915),
        new("Google Pixel 8 Pro", 448, 998),
        new("iPhone SE (3rd generation)", 375, 667),
        new("iPhone 15", 393, 852),
        new("iPhone 15 Pro", 393, 852),
        new("iPhone 15 Pro Max", 430, 932)
    ];
}