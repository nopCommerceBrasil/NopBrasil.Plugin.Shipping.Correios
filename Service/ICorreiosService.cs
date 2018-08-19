using Nop.Services.Shipping;

namespace NopBrasil.Plugin.Shipping.Correios.Service
{
    public interface ICorreiosService
    {
        WSCorreiosCalcPrecoPrazo.cResultado RequestCorreios(GetShippingOptionRequest getShippingOptionRequest);

        decimal GetConvertedRateToPrimaryCurrency(decimal rate);
    }
}
