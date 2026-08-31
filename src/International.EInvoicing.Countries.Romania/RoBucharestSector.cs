using System.Collections.Frozen;

namespace International.EInvoicing.Countries.Romania;

/// <summary>
/// The city names a Bucharest address may carry.
/// </summary>
/// <remarks>
/// <c>BR-RO-100</c> and its neighbours are fatal, and they surprise everyone: when a Romanian party's
/// country subdivision is <c>RO-B</c> — Bucharest — the <b>city name</b> must be the sector, spelled
/// <c>SECTOR1</c> to <c>SECTOR6</c>. Writing "Bucureşti" there, which is what every other country would want,
/// is what fails.
/// </remarks>
public static class RoBucharestSector
{
    /// <summary>The country subdivision code that triggers the rule.</summary>
    public const string Subdivision = "RO-B";

    private static readonly string[] Codes = ["SECTOR1", "SECTOR2", "SECTOR3", "SECTOR4", "SECTOR5", "SECTOR6"];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>The six sectors, in order.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>The city name for a sector, numbered one to six.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Bucharest has six sectors.</exception>
    public static string Of(int sector) => sector is >= 1 and <= 6
        ? Codes[sector - 1]
        : throw new ArgumentOutOfRangeException(
            nameof(sector),
            sector,
            "Bucharest has six sectors, numbered one to six.");

    /// <summary>Whether a city name is one the Bucharest rule accepts.</summary>
    public static bool IsSector(string? cityName) => cityName is not null && Known.Contains(cityName);
}
