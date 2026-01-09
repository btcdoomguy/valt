using LiteDB;

namespace Valt.Infra.DataAccess.LiteDBMappers;

/// <summary>
/// Custom DateTime mapper for LiteDB that ensures DateTime values are always
/// stored and retrieved as UTC. This prevents timezone-related comparison issues
/// when querying DateTime fields.
/// </summary>
public class DateTimeMapper
{
    public static void Register(BsonMapper mapper)
    {
        mapper.RegisterType<DateTime>(
            serialize: Serialize,
            deserialize: Deserialize
        );
    }

    private static BsonValue Serialize(DateTime dt)
    {
        // Pass through MinValue/MaxValue without conversion to avoid timezone shift issues
        if (dt == DateTime.MinValue || dt == DateTime.MaxValue)
            return new BsonValue(dt);

        // Ensure we store as UTC
        return new BsonValue(dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime());
    }

    private static DateTime Deserialize(BsonValue bson)
    {
        var dt = bson.AsDateTime;

        // Pass through MinValue/MaxValue without conversion
        if (dt == DateTime.MinValue || dt == DateTime.MaxValue)
            return dt;

        // Ensure we return as UTC (LiteDB returns Local by default)
        return DateTime.SpecifyKind(dt.ToUniversalTime(), DateTimeKind.Utc);
    }
}
