using System;
using System.Globalization;
using Nop.Core;
using Nop.Core.Domain.Shipping;
using Nop.Services.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;
using NopBrasil.Plugin.Shipping.Correios.Domain;
using NopBrasil.Plugin.Shipping.Correios.Service;

namespace NopBrasil.Plugin.Shipping.Correios
{
    public class CorreiosComputationMethod : BasePlugin, IShippingRateComputationMethod
    {
        private readonly ISettingService _settingService;
        private readonly CorreiosSettings _correiosSettings;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;
        private readonly ICorreiosService _correiosService;

        public CorreiosComputationMethod(ISettingService settingService,
            CorreiosSettings correiosSettings, ILogger logger, IWebHelper webHelper,
            ILocalizationService localizationService, ICorreiosService correiosService)
        {
            this._settingService = settingService;
            this._correiosSettings = correiosSettings;
            this._logger = logger;
            this._webHelper = webHelper;
            this._localizationService = localizationService;
            this._correiosService = correiosService;
        }

        private bool ValidateRequest(GetShippingOptionRequest getShippingOptionRequest, GetShippingOptionResponse response)
        {
            if (getShippingOptionRequest.Items == null)
                response.AddError(_localizationService.GetResource("Plugins.Shipping.Correios.Message.NoShipmentItems"));
            if (getShippingOptionRequest.ShippingAddress == null)
                response.AddError(_localizationService.GetResource("Plugins.Shipping.Correios.Message.AddressNotSet"));
            if (getShippingOptionRequest.CountryFrom == null)
                response.AddError(_localizationService.GetResource("Plugins.Shipping.Correios.Message.CountryNotSet"));
            if (getShippingOptionRequest.StateProvinceFrom == null)
                response.AddError(_localizationService.GetResource("Plugins.Shipping.Correios.Message.StateNotSet"));
            if (getShippingOptionRequest.ShippingAddress.ZipPostalCode == null)
                response.AddError(_localizationService.GetResource("Plugins.Shipping.Correios.Message.PostalCodeNotSet"));
            return response.Errors.Count > 0 ? false : true;
        }

        public GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException("getShippingOptionRequest");

            var response = new GetShippingOptionResponse();

            if (!ValidateRequest(getShippingOptionRequest, response))
                return response;

            try
            {
                WSCorreiosCalcPrecoPrazo.cResultado wsResult = _correiosService.RequestCorreios(getShippingOptionRequest);
                foreach (WSCorreiosCalcPrecoPrazo.cServico serv in wsResult?.Servicos)
                {
                    try
                    {
                        var obs = ValidateWSResult(serv);
                        response.ShippingOptions.Add(GetShippingOption(ApplyAdditionalFee(Convert.ToDecimal(serv.Valor, new CultureInfo("pt-BR"))), CorreiosServiceType.GetServiceName(serv.Codigo.ToString()), CalcPrazoEntrega(serv), obs));
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e.Message, e);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
            }

            if (response.ShippingOptions.Count <= 0)
                response.ShippingOptions.Add(GetShippingOption(_correiosSettings.ShippingRateDefault, _correiosSettings.ServiceNameDefault, _correiosSettings.QtdDaysForDeliveryDefault));

            return response;
        }

        private decimal ApplyAdditionalFee(decimal rate) => _correiosSettings.PercentageShippingFee > 0.0M ? rate * _correiosSettings.PercentageShippingFee : rate;

        private ShippingOption GetShippingOption(decimal rate, string serviceName, int prazo, string obs = null)
        {
            var shippingName = $"{serviceName} - {prazo} dia(s)";
            if (!string.IsNullOrEmpty(obs))
                shippingName += $" - {obs}";
            return new ShippingOption() { Rate = _correiosService.GetConvertedRateToPrimaryCurrency(rate), Name = shippingName };
        }

        private int CalcPrazoEntrega(WSCorreiosCalcPrecoPrazo.cServico serv)
        {
            int prazo = Convert.ToInt32(serv.PrazoEntrega);
            if (_correiosSettings.AddDaysForDelivery > 0)
                prazo += _correiosSettings.AddDaysForDelivery;
            return prazo;
        }

        private string ValidateWSResult(WSCorreiosCalcPrecoPrazo.cServico wsServico)
        {
            string retorno = string.Empty;
            if (!string.IsNullOrEmpty(wsServico.Erro) && (wsServico.Erro != "0"))
            {
                if ((wsServico.Erro == "009") || (wsServico.Erro == "010") || (wsServico.Erro == "011"))
                    retorno = wsServico.MsgErro;
                else
                    throw new NopException(wsServico.Erro + " - " + wsServico.MsgErro);
            }

            if (Convert.ToInt32(wsServico.PrazoEntrega) <= 0)
                throw new NopException(_localizationService.GetResource("Plugins.Shipping.Correios.Message.DeliveryUninformed"));

            if (Convert.ToDecimal(wsServico.Valor, new CultureInfo("pt-BR")) <= 0)
                throw new NopException(_localizationService.GetResource("Plugins.Shipping.Correios.Message.InvalidValueDelivery"));

            return retorno;
        }

        public decimal? GetFixedRate(GetShippingOptionRequest getShippingOptionRequest) => null;

        public override string GetConfigurationPageUrl() => _webHelper.GetStoreLocation() + "Admin/ShippingCorreios/Configure";

        public override void Install()
        {
            var settings = new CorreiosSettings()
            {
                Url = "http://ws.correios.com.br/calculador/CalcPrecoPrazo.asmx",
                PostalCodeFrom = "",
                CompanyCode = "",
                Password = "",
                AddDaysForDelivery = 0,
                PercentageShippingFee = 1.0M,
                DeclaredMinimumValue = 19.5M,
                MinimumWeight = 1,
                MaximumWeight = 30,
                MinimumHeight = 2.0M,
                MinimumLength = 16.0M,
                MinimumWidth = 11.0M,
                MaximumHeight = 105.0M,
                MaximumLength = 105.0M,
                MaximumWidth = 105.0M
            };
            _settingService.SaveSetting(settings);

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.Url", "URL");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.Url.Hint", "Specify Correios URL.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.PostalCodeFrom", "Postal Code From");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.PostalCodeFrom.Hint", "Specify From Postal Code.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.CompanyCode", "Company Code");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.CompanyCode.Hint", "Specify Your Company Code.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.Password", "Password");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.Password.Hint", "Specify Your Password.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.AddDaysForDelivery", "Additional Days For Delivery");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.AddDaysForDelivery.Hint", "Set The Amount Of Additional Days For Delivery.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.AvailableCarrierServices", "Available Carrier Services");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.AvailableCarrierServices.Hint", "Set Available Carrier Services.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.ServiceNameDefault", "Service Name Default");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.ServiceNameDefault.Hint", "Service Name Used When The Correios Does Not Return Value.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.ShippingRateDefault", "Shipping Rate Default");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.ShippingRateDefault.Hint", "Shipping Rate Used When The Correios Does Not Return Value.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.QtdDaysForDeliveryDefault", "Number Of Days For Delivery Default");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.QtdDaysForDeliveryDefault.Hint", "Number Of Days For Delivery Used When The Correios Does Not Return Value.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.PercentageShippingFee", "Additional percentage shipping fee");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.PercentageShippingFee.Hint", "Set the additional percentage shipping rate.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.DeclaredMinimumValue", "Declared Minimum Value");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.DeclaredMinimumValue.Hint", "The Minimum Amount Accepted by Correios for Declaration");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumLength", "Minimum Length");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumLength.Hint", "Set the Minimum Length");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumHeight", "Minimum Height");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumHeight.Hint", "Set the Minimum Height");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumWidth", "Minimum Width");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumWidth.Hint", "Set the Minimum Width");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumLength", "Maximum Length");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumLength.Hint", "Set the Maximum Length");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumHeight", "Maximum Height");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumHeight.Hint", "Set the Maximum Height");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumWidth", "Maximum Width");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumWidth.Hint", "Set the Maximum Width");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumWeight", "Minimum Weight");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumWeight.Hint", "Set the Minimum Weight");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumWeight", "Maximum Weight");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumWeight.Hint", "Set the Maximum Weight");

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.NoShipmentItems", "No shipment items");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.AddressNotSet", "Shipping address is not set");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.CountryNotSet", "Shipping country is not set");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.StateNotSet", "Shipping state is not set");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.PostalCodeNotSet", "Shipping zip postal code is not set");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.DeliveryUninformed", "Delivery uninformed");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Shipping.Correios.Message.InvalidValueDelivery", "Invalid value delivery");

            base.Install();
        }

        public override void Uninstall()
        {
            _settingService.DeleteSetting<CorreiosSettings>();

            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.Url");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.Url.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.PostalCodeFrom");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.PostalCodeFrom.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.CompanyCode");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.CompanyCode.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.Password");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.Password.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.AddDaysForDelivery");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.AddDaysForDelivery.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.AvailableCarrierServices");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.AvailableCarrierServices.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.ServiceNameDefault");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.ServiceNameDefault.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.ShippingRateDefault");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.ShippingRateDefault.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.QtdDaysForDeliveryDefault");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.QtdDaysForDeliveryDefault.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.PercentageShippingFee");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.PercentageShippingFee.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.DeclaredMinimumvalue");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.DeclaredMinimumvalue.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumLength");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumLength.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumHeight");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumHeight.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumWidth");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumWidth.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumLength");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumLength.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumHeight");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumHeight.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumWidth");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumWidth.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumWeight");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MinimumWeight.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumWeight");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Fields.MaximumWeight.Hint");

            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.NoShipmentItems");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.AddressNotSet");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.CountryNotSet");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.StateNotSet");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.PostalCodeNotSet");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.DeliveryUninformed");
            _localizationService.DeletePluginLocaleResource("Plugins.Shipping.Correios.Message.InvalidValueDelivery");
            base.Uninstall();
        }

        public ShippingRateComputationMethodType ShippingRateComputationMethodType => ShippingRateComputationMethodType.Realtime;

        public IShipmentTracker ShipmentTracker => new CorreiosShipmentTracker(_correiosSettings);
    }
}