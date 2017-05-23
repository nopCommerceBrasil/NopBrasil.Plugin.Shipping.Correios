using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Services.Directory;
using Nop.Services.Shipping;
using System;
using System.Text;
using System.Linq;
using Nop.Core.Caching;

namespace NopBrasil.Plugin.Shipping.Correios.Service
{
    public class CorreiosService : ICorreiosService
    {
        //colocar as unidades de medida e moeda utilizadas como configuração
        private const string MEASURE_WEIGHT_SYSTEM_KEYWORD = "kg";
        private const string MEASURE_DIMENSION_SYSTEM_KEYWORD = "centimeter";
        private const string CURRENCY_CODE = "BRL";
        //colocar o tamanho/peso mínimo/máximo permitido dos produtos como configuração

        //colocar cache nas pesquisas de medidas e peso
        private readonly ICacheManager _cacheManager;

        public CorreiosService(ICacheManager cacheManager)
        {
            this._cacheManager = cacheManager;
        }

        public int GetWheight(GetShippingOptionRequest shippingOptionRequest, IMeasureService measureService, IShippingService shippingService)
        {
            var usedMeasureWeight = measureService.GetMeasureWeightBySystemKeyword(MEASURE_WEIGHT_SYSTEM_KEYWORD);
            if (usedMeasureWeight == null)
                throw new NopException(string.Format("Correios shipping service. Could not load \"{0}\" measure weight", MEASURE_WEIGHT_SYSTEM_KEYWORD));

            int weight = Convert.ToInt32(Math.Ceiling(measureService.ConvertFromPrimaryMeasureWeight(shippingService.GetTotalWeight(shippingOptionRequest), usedMeasureWeight)));
            if (weight < 1)
                weight = 1;

            return weight;
        }

        public void GetDimensions(GetShippingOptionRequest shippingOptionRequest, IMeasureService measureService, IShippingService shippingService, out decimal width, out decimal length, out decimal height)
        {
            var usedMeasureDimension = measureService.GetMeasureDimensionBySystemKeyword(MEASURE_DIMENSION_SYSTEM_KEYWORD);
            if (usedMeasureDimension == null)
                throw new NopException(string.Format("Correios shipping service. Could not load \"{0}\" measure dimension", MEASURE_DIMENSION_SYSTEM_KEYWORD));

            shippingService.GetDimensions(shippingOptionRequest.Items, out width, out length, out height);

            length = measureService.ConvertFromPrimaryMeasureDimension(length, usedMeasureDimension);
            if (length < 16)
                length = 16;

            height = measureService.ConvertFromPrimaryMeasureDimension(height, usedMeasureDimension);
            if (height < 2)
                height = 2;

            width = measureService.ConvertFromPrimaryMeasureDimension(width, usedMeasureDimension);
            if (width < 11)
                width = 11;
        }

        public decimal GetConvertedRate(decimal rate, ICurrencyService currencyService, CurrencySettings currencySettings)
        {
            var usedCurrency = currencyService.GetCurrencyByCode(CURRENCY_CODE);
            if (usedCurrency == null)
                throw new NopException(string.Format("Correios shipping service. Could not load \"{0}\" currency", CURRENCY_CODE));

            if (usedCurrency.CurrencyCode == currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId).CurrencyCode)
                return rate;
            else
                return currencyService.ConvertToPrimaryStoreCurrency(rate, usedCurrency); //testar se não deve ser ConvertFromPrimaryStoreCurrency
        }

        public string GetSelectecServices(CorreiosSettings correioSettings)
        {
            StringBuilder sb = new StringBuilder();
            correioSettings.ServicesOffered.Split(':').ToList().ForEach(service => sb.Append(service?.Remove(0, 1).Replace(']', ',')));
            return sb.ToString().Remove(sb.ToString().Length - 1, 1);
        }
    }
}
