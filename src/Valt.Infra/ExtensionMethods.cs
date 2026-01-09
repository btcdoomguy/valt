using LiteDB;
using Valt.Core.Kernel.Ids;

namespace Valt.Infra;

public static class ExtensionMethods
{
    /// <summary>
    /// Converts a DateOnly to a DateTime at noon UTC.
    /// Using noon ensures the date remains stable across all timezone offsets (±12 hours).
    /// </summary>
    public static DateTime ToValtDateTime(this DateOnly dateOnly) =>
        new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day, 12, 0, 0, DateTimeKind.Utc);

    public static ObjectId ToObjectId(this CommonId id) => new(id.Value);
}