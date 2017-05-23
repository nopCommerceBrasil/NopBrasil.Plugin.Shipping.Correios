using System;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using NopBrasil.Plugin.Shipping.Correios.Models;
using Nop.Services.Configuration;
using Nop.Web.Framework.Controllers;
using NopBrasil.Plugin.Shipping.Correios.Domain;
using Nop.Web.Controllers;

namespace NopBrasil.Plugin.Shipping.Correios.Controllers
{
    [AdminAuthorize]
    public class ShippingCorreiosController : BasePublicController
    {
        private readonly CorreiosSettings _correiosSettings;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;

        public ShippingCorreiosController(CorreiosSettings correiosSettings, ISettingService settingService, IWebHelper webHelper)
        {
            this._correiosSettings = correiosSettings;
            this._settingService = settingService;
            this._webHelper = webHelper;
        }

        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new CorreiosShippingModel();
            model.Url = _correiosSettings.Url;
            model.PostalCodeFrom = _correiosSettings.PostalCodeFrom;
            model.CompanyCode = _correiosSettings.CompanyCode;
            model.Password = _correiosSettings.Password;
            model.AddDaysForDelivery = _correiosSettings.AddDaysForDelivery.ToString();
            model.ServiceNameDefault = _correiosSettings.ServiceNameDefault;
            model.ShippingRateDefault = _correiosSettings.ShippingRateDefault;
            model.QtdDaysForDeliveryDefault = _correiosSettings.QtdDaysForDeliveryDefault;

            var services = new CorreiosServiceType();
            string carrierServicesOfferedDomestic = _correiosSettings.ServicesOffered;
            foreach (string service in services.Services)
                model.AvailableCarrierServices.Add(service);

            if (!String.IsNullOrEmpty(carrierServicesOfferedDomestic))
                foreach (string service in services.Services)
                {
                    string serviceId = CorreiosServiceType.GetServiceId(service);
                    if (!String.IsNullOrEmpty(serviceId) && !String.IsNullOrEmpty(carrierServicesOfferedDomestic))
                    {
                        // Add delimiters [] so that single digit IDs aren't found in multi-digit IDs
                        if (carrierServicesOfferedDomestic.Contains(String.Format("[{0}]", serviceId)))
                            model.ServicesOffered.Add(service);
                    }
                }

            return View("~/Plugins/Shipping.Correios/Views/ShippingCorreios/Configure.cshtml", model);
        }

        [HttpPost]
        [ChildActionOnly]
        public ActionResult Configure(CorreiosShippingModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            //save settings
            _correiosSettings.Url = model.Url;
            _correiosSettings.PostalCodeFrom = model.PostalCodeFrom;
            _correiosSettings.CompanyCode = model.CompanyCode;
            _correiosSettings.Password = model.Password;
            _correiosSettings.AddDaysForDelivery = Convert.ToInt32(model.AddDaysForDelivery);
            _correiosSettings.ServiceNameDefault = model.ServiceNameDefault;
            _correiosSettings.ShippingRateDefault = model.ShippingRateDefault;
            _correiosSettings.QtdDaysForDeliveryDefault = model.QtdDaysForDeliveryDefault;

            // Save selected services
            var carrierServicesOfferedDomestic = new StringBuilder();
            int carrierServicesDomesticSelectedCount = 0;
            if (model.CheckedCarrierServices != null)
            {
                foreach (var cs in model.CheckedCarrierServices)
                {
                    carrierServicesDomesticSelectedCount++;
                    string serviceId = CorreiosServiceType.GetServiceId(cs);
                    if (!String.IsNullOrEmpty(serviceId))
                    {
                        // Add delimiters [] so that single digit IDs aren't found in multi-digit IDs
                        carrierServicesOfferedDomestic.AppendFormat("[{0}]:", serviceId);
                    }
                }
            }
            _correiosSettings.ServicesOffered = carrierServicesOfferedDomestic.ToString();

            _settingService.SaveSetting(_correiosSettings);
            return Configure();
        }
    }
}
