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

        public decimal PercentageShippingFee { get; set; }

        public decimal DeclaredMinimumValue { get; set; }

        public decimal MinimumLength { get; set; }

        public decimal MinimumHeight { get; set; }

        public decimal MinimumWidth { get; set; }

        public decimal MaximumLength { get; set; }

        public decimal MaximumHeight { get; set; }

        public decimal MaximumWidth { get; set; }

        public int MinimumWeight { get; set; }

        public int MaximumWeight { get; set; }
    }
}