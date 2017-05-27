namespace NopBrasil.Plugin.Shipping.Correios.Domain
{
    public class CorreiosServiceType
    {
        private string[] _services = {
                                        "Sedex",
                                        "Sedex a Cobrar",
                                        "Sedex 10",
                                        "Sedex Hoje",
                                        "PAC"
                                     };

        public string[] Services => _services;

        public static string GetServiceName(string serviceId)
        {
            string service = string.Empty;
            switch (serviceId)
            {
                case "40010":
                    service = "Sedex";
                    break;
                case "40045":
                    service = "Sedex a Cobrar";
                    break;
                case "40215":
                    service = "Sedex 10";
                    break;
                case "40290":
                    service = "Sedex Hoje";
                    break;
                case "41106":
                    service = "PAC";
                    break;
                default:
                    break;
            }
            return service;
        }

        public static string GetServiceId(string service)
        {
            string serviceId = string.Empty;
            switch (service)
            {
                case "Sedex":
                    serviceId = "40010";
                    break;
                case "Sedex a Cobrar":
                    serviceId = "40045";
                    break;
                case "Sedex 10":
                    serviceId = "40215";
                    break;
                case "Sedex Hoje":
                    serviceId = "40290";
                    break;
                case "PAC":
                    serviceId = "41106";
                    break;
                default:
                    break;
            }
            return serviceId;
        }
    }
}
