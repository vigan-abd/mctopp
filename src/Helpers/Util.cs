public static class Util
{
    public static string UniquePivot(string type, int tour, int pos)
    {
        return $"{type}-{tour}-{pos}";
    }

    public static string ExtractPivot(string key)
    {
        return key.Split('-')[0];
    }
}