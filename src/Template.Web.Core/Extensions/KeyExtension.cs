namespace Template.Web.Domain.Shared.Utilities;

public static class KeyExtension
{
    public static dynamic[] ToDynamicArray(this IEnumerable<long> ids)
    {
        return ids.Select(id => (dynamic)id).ToArray();
    }

    /// <summary>
    /// 判断guid是否为空
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public static bool IsNullableGuidEmpty(this Guid? guid)
    {
        return guid == null || guid == Guid.Empty;
    }

    /// <summary>
    /// 判断guid是否为空
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public static bool IsGuidEmpty(this Guid guid)
    {
        return guid == Guid.Empty;
    }
}