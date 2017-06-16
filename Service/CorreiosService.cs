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

namespace NopBrasil.Plugin.Shipping.Correios.Service
{
    public class CorreiosService : ICorreiosService
    {
        //colocar as unidades de medida e moeda utilizadas como configuração
        private const string MEASURE_WEIGHT_SYSTEM_KEYWORD = "kg";
        private const string MEASURE_DIMENSION_SYSTEM_KEYWORD = "centimeter";
        private const string CURRENCY_CODE = "BRL";
        //colocar o tamanho/peso mínimo/máximo permitido dos produtos como configuração

        private readonly IMeasureService _measureService;
        private readonly IShippingService _shippingService;
        private readonly CorreiosSettings _correiosSettings;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;

        public CorreiosService(IMeasureService measureService, IShippingService shippingService, CorreiosSettings correiosSettings,
            ICurrencyService currencyService, CurrencySettings currencySettings)
        {
            this._measureService = measureService;
            this._shippingService = shippingService;
            this._correiosSettings = correiosSettings;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
        }

        public WSCorreiosCalcPrecoPrazo.cResultado RequestCorreios(GetShippingOptionRequest getShippingOptionRequest)
        {
            Binding binding = new BasicHttpBinding();
            binding.Name = "CalcPrecoPrazoWSSoap";

            if (string.IsNullOrEmpty(getShippingOptionRequest.ZipPostalCodeFrom))
                getShippingOptionRequest.ZipPostalCodeFrom = _correiosSettings.PostalCodeFrom;

            decimal length, width, height;
            GetDimensions(getShippingOptionRequest, out width, out length, out height);

            

            EndpointAddress endpointAddress = new EndpointAddress(_correiosSettings.Url);

            WSCorreiosCalcPrecoPrazo.CalcPrecoPrazoWSSoap wsCorreios = new WSCorreiosCalcPrecoPrazo.CalcPrecoPrazoWSSoapClient(binding, endpointAddress);
            return wsCorreios.CalcPrecoPrazo(_correiosSettings.CompanyCode, _correiosSettings.Password, GetSelectecServices(_correiosSettings), getShippingOptionRequest.ZipPostalCodeFrom,
                getShippingOptionRequest.ShippingAddress.ZipPostalCode, GetWheight(getShippingOptionRequest).ToString(), 1, length, height, width, 0, "N", GetDeclaredValue(getShippingOptionRequest), "N");
        }

        private decimal GetDeclaredValue(GetShippingOptionRequest shippingOptionRequest)
        {
            decimal declaredValue = GetConvertedRateFromPrimaryCurrency(shippingOptionRequest.Items.Sum(item => item.ShoppingCartItem.Product.Price));
            return declaredValue < 18.0M ? 18.0M : declaredValue;
        }

        private int GetWheight(GetShippingOptionRequest shippingOptionRequest)
        {
            var usedMeasureWeight = _measureService.GetMeasureWeightBySystemKeyword(MEASURE_WEIGHT_SYSTEM_KEYWORD);
            if (usedMeasureWeight == null)
                throw new NopException($"Correios shipping service. Could not load \"{MEASURE_WEIGHT_SYSTEM_KEYWORD}\" measure weight");

            int weight = Convert.ToInt32(Math.Ceiling(_measureService.ConvertFromPrimaryMeasureWeight(_shippingService.GetTotalWeight(shippingOptionRequest), usedMeasureWeight)));
            return weight < 1 ? 1 : weight;
        }

        private void GetDimensions(GetShippingOptionRequest shippingOptionRequest, out decimal width, out decimal length, out decimal height)
        {
            var usedMeasureDimension = _measureService.GetMeasureDimensionBySystemKeyword(MEASURE_DIMENSION_SYSTEM_KEYWORD);
            if (usedMeasureDimension == null)
                throw new NopException($"Correios shipping service. Could not load \"{MEASURE_DIMENSION_SYSTEM_KEYWORD}\" measure dimension");

            _shippingService.GetDimensions(shippingOptionRequest.Items, out width, out length, out height);

            length = _measureService.ConvertFromPrimaryMeasureDimension(length, usedMeasureDimension);
            if (length < 16)
                length = 16;

            height = _measureService.ConvertFromPrimaryMeasureDimension(height, usedMeasureDimension);
            if (height < 2)
                height = 2;

            width = _measureService.ConvertFromPrimaryMeasureDimension(width, usedMeasureDimension);
            if (width < 11)
                width = 11;
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
    }
}
