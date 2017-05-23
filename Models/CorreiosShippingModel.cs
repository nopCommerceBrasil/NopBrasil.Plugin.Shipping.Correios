using System.Collections.Generic;
using Nop.Web.Framework;

namespace NopBrasil.Plugin.Shipping.Correios.Models
{
    public class CorreiosShippingModel
    {
        public CorreiosShippingModel()
        {
            ServicesOffered = new List<string>();
            AvailableCarrierServices = new List<string>();
        }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.Url")]
        public string Url { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.PostalCodeFrom")]
        public string PostalCodeFrom { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.CompanyCode")]
        public string CompanyCode { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.Password")]
        public string Password { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.AddDaysForDelivery")]
        public string AddDaysForDelivery { get; set; }

        public IList<string> ServicesOffered { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.AvailableCarrierServices")]
        public IList<string> AvailableCarrierServices { get; set; }
        public string[] CheckedCarrierServices { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.ServiceNameDefault")]
        public string ServiceNameDefault { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.ShippingRateDefault")]
        public decimal ShippingRateDefault { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.QtdDaysForDeliveryDefault")]
        public int QtdDaysForDeliveryDefault { get; set; }
    }
}