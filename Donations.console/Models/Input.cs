namespace Donations.Console.Models;

public record Input
{
    public string? Name { get; init; }
    public string? Email { get; init; }
}