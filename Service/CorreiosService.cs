using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Services.Directory;
using Nop.Services.Shipping;
using System;
using System.Text;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel;
using NopBrasil.Plugin.Shipping.Correios.Utils;
using Nop.Services.Common;

namespace NopBrasil.Plugin.Shipping.Correios.Service
{
    public class CorreiosService : ICorreiosService
    {
        //colocar as unidades de medida e moeda utilizadas como configuração
        private const string MEASURE_WEIGHT_SYSTEM_KEYWORD = "kg";
        private const string MEASURE_DIMENSION_SYSTEM_KEYWORD = "centimeter";
        private const string CURRENCY_CODE = "BRL";

        private readonly IMeasureService _measureService;
        private readonly IShippingService _shippingService;
        private readonly CorreiosSettings _correiosSettings;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IAddressService _addressService;

        public CorreiosService(IMeasureService measureService, IShippingService shippingService, CorreiosSettings correiosSettings,
            ICurrencyService currencyService, CurrencySettings currencySettings, IAddressService addressService)
        {
            this._measureService = measureService;
            this._shippingService = shippingService;
            this._correiosSettings = correiosSettings;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._addressService = addressService;
        }

        public WSCorreiosCalcPrecoPrazo.cResultado RequestCorreios(GetShippingOptionRequest getShippingOptionRequest)
        {
            Binding binding = new BasicHttpBinding();
            binding.Name = "CalcPrecoPrazoWSSoap";

            getShippingOptionRequest.ZipPostalCodeFrom = GetZipPostalCodeFrom(getShippingOptionRequest);

            decimal length, width, height;
            GetDimensions(getShippingOptionRequest, out width, out length, out height);

            EndpointAddress endpointAddress = new EndpointAddress(_correiosSettings.Url);

            WSCorreiosCalcPrecoPrazo.CalcPrecoPrazoWSSoap wsCorreios = new WSCorreiosCalcPrecoPrazo.CalcPrecoPrazoWSSoapClient(binding, endpointAddress);
            return wsCorreios.CalcPrecoPrazo(_correiosSettings.CompanyCode, _correiosSettings.Password, GetSelectecServices(_correiosSettings), getShippingOptionRequest.ZipPostalCodeFrom,
                getShippingOptionRequest.ShippingAddress.ZipPostalCode, GetWheight(getShippingOptionRequest).ToString(), 1, length, height, width, 0, "N", GetDeclaredValue(getShippingOptionRequest), "N");
        }

        private string GetZipPostalCodeFrom(GetShippingOptionRequest getShippingOptionRequest)
        {
            if ((getShippingOptionRequest.WarehouseFrom != null) && (!string.IsNullOrEmpty(_addressService.GetAddressById(getShippingOptionRequest.WarehouseFrom.AddressId)?.ZipPostalCode)))
                return  _addressService.GetAddressById(getShippingOptionRequest.WarehouseFrom.AddressId).ZipPostalCode;
            if (!string.IsNullOrEmpty(getShippingOptionRequest.ZipPostalCodeFrom))
                return getShippingOptionRequest.ZipPostalCodeFrom;
            return _correiosSettings.PostalCodeFrom;
        }

        private decimal GetDeclaredValue(GetShippingOptionRequest shippingOptionRequest)
        {
            decimal declaredValue = GetConvertedRateFromPrimaryCurrency(shippingOptionRequest.Items.Sum(item => item.Product.Price));
            return Math.Max(declaredValue, _correiosSettings.DeclaredMinimumValue);
        }

        private int GetWheight(GetShippingOptionRequest shippingOptionRequest)
        {
            var usedMeasureWeight = _measureService.GetMeasureWeightBySystemKeyword(MEASURE_WEIGHT_SYSTEM_KEYWORD);
            if (usedMeasureWeight == null)
                throw new NopException($"Correios shipping service. Could not load \"{MEASURE_WEIGHT_SYSTEM_KEYWORD}\" measure weight");

            int weight = Convert.ToInt32(Math.Ceiling(_measureService.ConvertFromPrimaryMeasureWeight(_shippingService.GetTotalWeight(shippingOptionRequest), usedMeasureWeight)));
            return AcceptedDimensions(weight, _correiosSettings.MinimumWeight, _correiosSettings.MaximumWeight);
        }

        private void GetDimensions(GetShippingOptionRequest shippingOptionRequest, out decimal width, out decimal length, out decimal height)
        {
            var usedMeasureDimension = _measureService.GetMeasureDimensionBySystemKeyword(MEASURE_DIMENSION_SYSTEM_KEYWORD);
            if (usedMeasureDimension == null)
                throw new NopException($"Correios shipping service. Could not load \"{MEASURE_DIMENSION_SYSTEM_KEYWORD}\" measure dimension");

            _shippingService.GetDimensions(shippingOptionRequest.Items, out width, out length, out height);

            length = _measureService.ConvertFromPrimaryMeasureDimension(length, usedMeasureDimension);
            length = AcceptedDimensions(length, _correiosSettings.MinimumLength, _correiosSettings.MaximumLength);

            height = _measureService.ConvertFromPrimaryMeasureDimension(height, usedMeasureDimension);
            height = AcceptedDimensions(height, _correiosSettings.MinimumHeight, _correiosSettings.MaximumHeight);

            width = _measureService.ConvertFromPrimaryMeasureDimension(width, usedMeasureDimension);
            width = AcceptedDimensions(width, _correiosSettings.MinimumWidth, _correiosSettings.MaximumWidth);
        }

        public decimal GetConvertedRateFromPrimaryCurrency(decimal rate) => GetConvertedRate(rate, _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId), GetSupportedCurrency());

        public decimal GetConvertedRateToPrimaryCurrency(decimal rate) => GetConvertedRate(rate, GetSupportedCurrency(), _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId));

        private decimal GetConvertedRate(decimal rate, Currency source, Currency target) => (source.CurrencyCode == target.CurrencyCode) ? rate : _currencyService.ConvertCurrency(rate, source, target);

        private Currency GetSupportedCurrency()
        {
            var currency = _currencyService.GetCurrencyByCode(CURRENCY_CODE);
            if (currency == null)
                throw new NopException($"Correios shipping service. Could not load \"{CURRENCY_CODE}\" currency");
            return currency;
        }

        private string GetSelectecServices(CorreiosSettings correioSettings)
        {
            StringBuilder sb = new StringBuilder();
            correioSettings.ServicesOffered.RemoveLastIfEndsWith(":").Split(':').ToList().ForEach(service => sb.Append(service?.Remove(0, 1).Replace(']', ',')));
            return sb.ToString().Remove(sb.ToString().Length - 1, 1);
        }

        private decimal AcceptedDimensions(decimal value, decimal minimum, decimal maximum) => Math.Min(Math.Max(value, minimum), maximum);

        private int AcceptedDimensions(int value, int minimum, int maximum) => Math.Min(Math.Max(value, minimum), maximum);
    }
}
