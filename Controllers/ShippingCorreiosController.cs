using System;
using System.Text;
using Nop.Core;
using NopBrasil.Plugin.Shipping.Correios.Models;
using Nop.Services.Configuration;
using Nop.Web.Framework.Controllers;
using NopBrasil.Plugin.Shipping.Correios.Domain;
using NopBrasil.Plugin.Shipping.Correios.Utils;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Services.Localization;

namespace NopBrasil.Plugin.Shipping.Correios.Controllers
{
    [Area(AreaNames.Admin)]
    public class ShippingCorreiosController : BasePluginController
    {
        private readonly CorreiosSettings _correiosSettings;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;

        public ShippingCorreiosController(CorreiosSettings correiosSettings, ISettingService settingService, IWebHelper webHelper, ILocalizationService localizationService)
        {
            this._correiosSettings = correiosSettings;
            this._settingService = settingService;
            this._webHelper = webHelper;
            this._localizationService = localizationService;
        }

        public ActionResult Configure()
        {
            var model = new CorreiosShippingModel
            {
                Url = _correiosSettings.Url,
                PostalCodeFrom = _correiosSettings.PostalCodeFrom,
                CompanyCode = _correiosSettings.CompanyCode,
                Password = _correiosSettings.Password,
                AddDaysForDelivery = _correiosSettings.AddDaysForDelivery.ToString(),
                ServiceNameDefault = _correiosSettings.ServiceNameDefault,
                ShippingRateDefault = _correiosSettings.ShippingRateDefault,
                QtdDaysForDeliveryDefault = _correiosSettings.QtdDaysForDeliveryDefault,
                PercentageShippingFee = _correiosSettings.PercentageShippingFee
            };

            foreach (string service in CorreiosServiceType.Services)
                model.AvailableCarrierServices.Add(service);

            LoadSavedServices(model);

            return View("~/Plugins/Shipping.Correios/Views/Configure.cshtml", model);
        }

        private void LoadSavedServices(CorreiosShippingModel model)
        {
            if (!string.IsNullOrEmpty(_correiosSettings.ServicesOffered))
                foreach (string service in CorreiosServiceType.Services)
                {
                    string serviceId = CorreiosServiceType.GetServiceId(service);
                    if (!string.IsNullOrEmpty(serviceId) && !string.IsNullOrEmpty(_correiosSettings.ServicesOffered))
                        if (_correiosSettings.ServicesOffered.Contains($"[{serviceId}]")) // Add delimiters [] so that single digit IDs aren't found in multi-digit IDs
                            model.ServicesOffered.Add(service);
                }
        }

        [HttpPost]
        public ActionResult Configure(CorreiosShippingModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            _correiosSettings.Url = model.Url;
            _correiosSettings.PostalCodeFrom = model.PostalCodeFrom;
            _correiosSettings.CompanyCode = model.CompanyCode;
            _correiosSettings.Password = model.Password;
            _correiosSettings.AddDaysForDelivery = Convert.ToInt32(model.AddDaysForDelivery);
            _correiosSettings.ServiceNameDefault = model.ServiceNameDefault;
            _correiosSettings.ShippingRateDefault = model.ShippingRateDefault;
            _correiosSettings.QtdDaysForDeliveryDefault = model.QtdDaysForDeliveryDefault;
            _correiosSettings.PercentageShippingFee = model.PercentageShippingFee;
            _correiosSettings.ServicesOffered = GetSelectedServices(model);
            _settingService.SaveSetting(_correiosSettings);
            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));
            return Configure();
        }

        private string GetSelectedServices(CorreiosShippingModel model)
        {
            var carrierServicesOfferedDomestic = new StringBuilder();
            if (model.CheckedCarrierServices != null)
                foreach (var cs in model.CheckedCarrierServices)
                {
                    string serviceId = CorreiosServiceType.GetServiceId(cs);
                    if (!string.IsNullOrEmpty(serviceId))
                        carrierServicesOfferedDomestic.Append($"[{serviceId}]:"); // Add delimiters [] so that single digit IDs aren't found in multi-digit IDs
                }
            return carrierServicesOfferedDomestic.ToString().RemoveLastIfEndsWith(":");
        }
    }
}
