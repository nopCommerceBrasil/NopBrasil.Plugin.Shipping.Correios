using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System.Collections.Generic;

namespace NopBrasil.Plugin.Shipping.Correios.Models
{
    public class CorreiosShippingModel : BaseNopModel
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

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.PercentageShippingFee")]
        public decimal PercentageShippingFee { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.DeclaredMinimumValue")]
        public decimal DeclaredMinimumValue { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.MinimumLength")]
        public decimal MinimumLength { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.MinimumHeight")]
        public decimal MinimumHeight { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.MinimumWidth")]
        public decimal MinimumWidth { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.MaximumLength")]
        public decimal MaximumLength { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.MaximumHeight")]
        public decimal MaximumHeight { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.MaximumWidth")]
        public decimal MaximumWidth { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.MinimumWeight")]
        public int MinimumWeight { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Correios.Fields.MaximumWeight")]
        public int MaximumWeight { get; set; }
    }
}