using Nop.Core.Domain.Directory;
using Nop.Services.Directory;
using Nop.Services.Shipping;

namespace NopBrasil.Plugin.Shipping.Correios.Service
{
    public interface ICorreiosService
    {
        WSCorreiosCalcPrecoPrazo.cResultado RequestCorreios(GetShippingOptionRequest getShippingOptionRequest);

        decimal GetConvertedRate(decimal rate);
    }
}
