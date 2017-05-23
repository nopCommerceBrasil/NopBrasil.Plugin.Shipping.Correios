using Nop.Core.Configuration;

namespace NopBrasil.Plugin.Shipping.Correios
{
    public class CorreiosSettings : ISettings
    {
        public string Url { get; set; }

        public string PostalCodeFrom { get; set; }

        public string CompanyCode { get; set; }

        public string Password { get; set; }

        public int AddDaysForDelivery { get; set; }

        public string ServicesOffered { get; set; }

        public string ServiceNameDefault { get; set; }

        public decimal ShippingRateDefault { get; set; }

        public int QtdDaysForDeliveryDefault { get; set; }
    }
}