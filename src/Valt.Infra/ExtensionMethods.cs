using LiteDB;
using Valt.Core.Kernel.Ids;

namespace Valt.Infra;

public static class ExtensionMethods
{
    public static DateTime ToValtDateTime(this DateOnly dateOnly) =>
        dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);

    public static ObjectId ToObjectId(this CommonId id) => new(id.Value);
}