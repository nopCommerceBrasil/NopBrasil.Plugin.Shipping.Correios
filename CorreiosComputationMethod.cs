using System;
using System.Globalization;
using System.Linq;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;
using NopBrasil.Plugin.Shipping.Correios.Domain;
using NopBrasil.Plugin.Shipping.Correios.Service;
using System.ServiceModel.Channels;
using System.ServiceModel;

namespace NopBrasil.Plugin.Shipping.Correios
{
    public class CorreiosComputationMethod : BasePlugin, IShippingRateComputationMethod
    {
        private readonly IMeasureService _measureService;
        private readonly IShippingService _shippingService;
        private readonly ISettingService _settingService;
        private readonly CorreiosSettings _correiosSettings;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly ILogger _logger;
        private readonly ILocalizationService _localizationService;
        private readonly ICorreiosService _correiosService;

        public CorreiosComputationMethod(IMeasureService measureService,
            IShippingService shippingService, ISettingService settingService,
            CorreiosSettings correiosSettings, ICurrencyService currencyService, 
            CurrencySettings currencySettings, ILogger logger,
            ILocalizationService localizationService, ICorreiosService correiosService)
        {
            this._measureService = measureService;
            this._shippingService = shippingService;
            this._settingService = settingService;
            this._correiosSettings = correiosSettings;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._logger = logger;
            this._localizationService = localizationService;
            this._correiosService = correiosService;
        }

        //refactorar método
        public GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException("getShippingOptionRequest");

            var response = new GetShippingOptionResponse();

            if (getShippingOptionRequest.Items == null)
            {
                response.AddError(_localizationService.GetResource("Plugins.Shipping.Correios.Message.NoShipmentItems"));
                return response;
            }

            if (getShippingOptionRequest.ShippingAddress == null)
            {
                response.AddError(_localizationService.GetResource("Plugins.Shipping.Correios.Message.AddressNotSet"));
                return response;
            }

            if (getShippingOptionRequest.ShippingAddress.Country == null)
            {
                response.AddError(_localizationService.GetResource("Plugins.Shipping.Correios.Message.CountryNotSet"));
                return response;
            }

            if (getShippingOptionRequest.ShippingAddress.StateProvince == null)
            {
                response.AddError(_localizationService.GetResource("Plugins.Shipping.Correios.Message.StateNotSet"));
                return response;
            }

            if (getShippingOptionRequest.ShippingAddress.ZipPostalCode == null)
            {
                response.AddError(_localizationService.GetResource("Plugins.Shipping.Correios.Message.PostalCodeNotSet"));
                return response;
            }

            try
            {
                if (string.IsNullOrEmpty(getShippingOptionRequest.ZipPostalCodeFrom))
                    getShippingOptionRequest.ZipPostalCodeFrom = _correiosSettings.PostalCodeFrom;

                string selectedServices = _correiosService.GetSelectecServices(_correiosSettings);

                Binding binding = new BasicHttpBinding();
                binding.Name = "CalcPrecoPrazoWSSoap";

                decimal length, width, height;
                _correiosService.GetDimensions(getShippingOptionRequest, _measureService, _shippingService, out width, out length, out height);

                decimal valuePackage = getShippingOptionRequest.Items.Sum(item => item.ShoppingCartItem.Product.Price);

                EndpointAddress endpointAddress = new EndpointAddress(_correiosSettings.Url);

                WSCorreiosCalcPrecoPrazo.CalcPrecoPrazoWSSoap wsCorreios = new WSCorreiosCalcPrecoPrazo.CalcPrecoPrazoWSSoapClient(binding, endpointAddress);
                WSCorreiosCalcPrecoPrazo.cResultado wsResult = wsCorreios.CalcPrecoPrazo(_correiosSettings.CompanyCode, _correiosSettings.Password,
                    selectedServices, _correiosSettings.PostalCodeFrom, getShippingOptionRequest.ShippingAddress.ZipPostalCode,
                        _correiosService.GetWheight(getShippingOptionRequest, _measureService, _shippingService).ToString(), 1,
                            length, height, width, 0, "N", valuePackage, "N");

                if (wsResult != null)
                {
                    foreach (WSCorreiosCalcPrecoPrazo.cServico serv in wsResult.Servicos)
                    {
                        try
                        {
                            ValidateWSResult(serv);

                            int prazo = Convert.ToInt32(serv.PrazoEntrega);
                            if (_correiosSettings.AddDaysForDelivery > 0)
                                prazo += _correiosSettings.AddDaysForDelivery;

                            ShippingOption shippingOption = new ShippingOption();
                            shippingOption.Rate = _correiosService.GetConvertedRate(Convert.ToDecimal(serv.Valor, new CultureInfo("pt-BR")), _currencyService, _currencySettings);
                            shippingOption.Name = CorreiosServiceType.GetServiceName(serv.Codigo.ToString()) + " - " + prazo.ToString() + " dia(s)";
                            response.ShippingOptions.Add(shippingOption);
                        }
                        catch (Exception e)
                        {
                            response.AddError(e.Message);
                        }
                    }
                }
                else
                {
                    response.AddError(_localizationService.GetResource("Plugins.Shipping.Correios.Message.ErrorConnectCorreios"));
                }

                if (response.ShippingOptions.Count <= 0)
                {
                    ShippingOption shippingOption = new ShippingOption();
                    shippingOption.Rate = _correiosService.GetConvertedRate(_correiosSettings.ShippingRateDefault, _currencyService, _currencySettings);
                    shippingOption.Name = _correiosSettings.ServiceNameDefault + " - " + _correiosSettings.QtdDaysForDeliveryDefault.ToString() + " dia(s)";
                    response.ShippingOptions.Add(shippingOption);
                }
            }
            catch (Exception e)
            {
                response.AddError(_localizationService.GetResource("Plugins.Shipping.Correios.Message.ErrorCalculateRate"));
                _logger.Error(e.Message, e);
            }

            return response;
        }

        private void ValidateWSResult(WSCorreiosCalcPrecoPrazo.cServico wsServico)
        {
            if (string.IsNullOrEmpty(wsServico.Erro))
                throw new NopException(wsServico.Erro + " - " + wsServico.MsgErro);

            if (Convert.ToInt32(wsServico.PrazoEntrega) <= 0)
                throw new NopException(_localizationService.GetResource("Plugins.Shipping.Correios.Message.DeliveryUninformed"));

            if (Convert.ToDecimal(wsServico.Valor, new CultureInfo("pt-BR")) <= 0)
                throw new NopException(_localizationService.GetResource("Plugins.Shipping.Correios.Message.InvalidValueDelivery"));
        }

        public decimal? GetFixedRate(GetShippingOptionRequest getShippingOptionRequest)
        {
            return null;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "ShippingCorreios";
            routeValues = new RouteValueDictionary() { { "Namespaces", "NopBrasil.Plugin.Shipping.Correios.Controllers" }, { "area", null } };
        }

        public override void Install()
        {
            var settings = new CorreiosSettings()
            {
                Url = "http://ws.correios.com.br/calculador/CalcPrecoPrazo.asmx",
                PostalCodeFrom = "",
                CompanyCode = "",
                Password = "",
                AddDaysForDelivery = 0
            };
            _settingService.SaveSetting(settings);

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.Url", "URL");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.Url.Hint", "Specify Correios URL.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.PostalCodeFrom", "Postal Code From");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.PostalCodeFrom.Hint", "Specify From Postal Code.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.CompanyCode", "Company Code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.CompanyCode.Hint", "Specify Your Company Code.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.Password", "Password");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.Password.Hint", "Specify Your Password.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.AddDaysForDelivery", "Additional Days For Delivery");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.AddDaysForDelivery.Hint", "Set The Amount Of Additional Days For Delivery.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.AvailableCarrierServices", "Available Carrier Services");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.AvailableCarrierServices.Hint", "Set Available Carrier Services.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.ServiceNameDefault", "Service Name Default");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.ServiceNameDefault.Hint", "Service Name Used When The Correios Does Not Return Value.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.ShippingRateDefault", "Shipping Rate Default");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.ShippingRateDefault.Hint", "Shipping Rate Used When The Correios Does Not Return Value.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.QtdDaysForDeliveryDefault", "Number Of Days For Delivery Default");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.QtdDaysForDeliveryDefault.Hint", "Number Of Days For Delivery Used When The Correios Does Not Return Value.");

            //ajustar mensagens de erro
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.NoShipmentItems", "No shipment items");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.AddressNotSet", "Shipping address is not set");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.CountryNotSet", "Shipping country is not set");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.StateNotSet", "Shipping state is not set");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.PostalCodeNotSet", "Shipping zip postal code is not set");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.ErrorConnectCorreios", "Error trying connect to Correios");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.ErrorCalculateRate", "Error on calculate the shipping rate");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.DeliveryUninformed", "Delivery uninformed");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.InvalidValueDelivery", "Invalid value delivery");

            base.Install();
        }

        public override void Uninstall()
        {
            _settingService.DeleteSetting<CorreiosSettings>();

            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.Url");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.Url.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.PostalCodeFrom");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.PostalCodeFrom.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.CompanyCode");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.CompanyCode.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.Password");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.Password.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.AddDaysForDelivery");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.AddDaysForDelivery.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.AvailableCarrierServices");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.AvailableCarrierServices.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.ServiceNameDefault");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.ServiceNameDefault.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.ShippingRateDefault");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.ShippingRateDefault.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.QtdDaysForDeliveryDefault");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.QtdDaysForDeliveryDefault.Hint");

            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.NoShipmentItems");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.AddressNotSet");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.CountryNotSet");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.StateNotSet");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.PostalCodeNotSet");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.ErrorConnectCorreios");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.ErrorCalculateRate");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.DeliveryUninformed");
            this.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.InvalidValueDelivery");

            base.Uninstall();
        }

        public ShippingRateComputationMethodType ShippingRateComputationMethodType
        {
            get { return ShippingRateComputationMethodType.Realtime; }
        }

        public IShipmentTracker ShipmentTracker
        {
            get { return new CorreiosShipmentTracker(_correiosSettings); }
        }
    }
}