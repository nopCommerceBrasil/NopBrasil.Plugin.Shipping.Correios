namespace NopBrasil.Plugin.Shipping.Correios.Domain
{
    public static class CorreiosServiceType
    {
        public static string[] Services { get; } = {
                                        "Sedex",
                                        "Sedex a Cobrar",
                                        "Sedex 10",
                                        "Sedex Hoje",
                                        "PAC"
                                     };

        public static string GetServiceName(string serviceId)
        {
            switch (serviceId)
            {
                case "40010":
                    return "Sedex";
                case "40045":
                    return "Sedex a Cobrar";
                case "40215":
                    return "Sedex 10";
                case "40290":
                    return "Sedex Hoje";
                case "41106":
                    return "PAC";
                default:
                    return string.Empty;
            }
        }

        public static string GetServiceId(string service)
        {
            switch (service)
            {
                case "Sedex":
                    return "40010";
                case "Sedex a Cobrar":
                    return "40045";
                case "Sedex 10":
                    return "40215";
                case "Sedex Hoje":
                    return "40290";
                case "PAC":
                    return "41106";
                default:
                    return string.Empty;
            }
        }
    }
}