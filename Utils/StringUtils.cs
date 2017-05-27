namespace NopBrasil.Plugin.Shipping.Correios.Utils
{
    public static class StringUtils
    {
        public static string RemoveLastIfEndsWith(this string str, string strToRemove)
        {
            if (str.EndsWith(strToRemove))
                return str.Remove(str.Length - 1, 1);
            return str;
        }
    }
}
