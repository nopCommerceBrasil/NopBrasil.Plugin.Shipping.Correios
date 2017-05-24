using Nop.Core.Domain.Directory;
using Nop.Services.Directory;
using Nop.Services.Shipping;

namespace NopBrasil.Plugin.Shipping.Correios.Service
{
    public interface ICorreiosService
    {
        WSCorreiosCalcPrecoPrazo.cResultado RequestCorreios(GetShippingOptionRequest getShippingOptionRequest, string selectedServices);

        //int GetWheight(GetShippingOptionRequest shippingOptionRequest, IMeasureService measureService, IShippingService shippingService);

        //void GetDimensions(GetShippingOptionRequest shippingOptionRequest, IMeasureService measureService, IShippingService shippingService, out decimal width, out decimal length, out decimal height);

        decimal GetConvertedRate(decimal rate, ICurrencyService currencyService, CurrencySettings currencySettings);

        string GetSelectecServices(CorreiosSettings correioSettings);
    }
}
