using Autofac;
using Autofac.Core;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using NopBrasil.Plugin.Shipping.Correios.Controllers;
using NopBrasil.Plugin.Shipping.Correios.Service;

namespace NopBrasil.Plugin.Shipping.Correios.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig nopConfig)
        {
            //we cache presentation models between requests
            builder.RegisterType<ShippingCorreiosController>().AsSelf();
            builder.RegisterType<CorreiosService>().As<ICorreiosService>().WithParameter(ResolvedParameter.ForNamed<ICacheManager>("nop_cache_static")).InstancePerDependency();
        }

        public int Order
        {
            get { return 2; }
        }
    }
}
