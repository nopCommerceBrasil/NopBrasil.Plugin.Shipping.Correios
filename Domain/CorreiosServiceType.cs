namespace NopBrasil.Plugin.Shipping.Correios.Domain
{
    public static class CorreiosServiceType
    {
        public static string[] Services { get; } = {
                                        "Sedex",
                                        "Sedex a Cobrar",
                                        "Sedex 10",
                                        "Sedex Hoje",
                                        "PAC",
                                        "PAC a Cobrar"
                                     };

        public static string GetServiceName(string serviceId)
        {
            switch (serviceId.PadLeft(5, '0'))
            {
                case "04014":
                    return "Sedex";
                case "04065":
                    return "Sedex a Cobrar";
                case "40215":
                    return "Sedex 10";
                case "40290":
                    return "Sedex Hoje";
                case "04510":
                    return "PAC";
                case "04707":
                    return "PAC a Cobrar";
                default:
                    return string.Empty;
            }
        }

        public static string GetServiceId(string service)
        {
            switch (service)
            {
                case "Sedex":
                    return "04014";
                case "Sedex a Cobrar":
                    return "04065";
                case "Sedex 10":
                    return "40215";
                case "Sedex Hoje":
                    return "40290";
                case "PAC":
                    return "04510";
                case "PAC a Cobrar":
                    return "04707";
                default:
                    return string.Empty;
            }
        }
    }
}