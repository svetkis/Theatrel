using Autofac;

namespace theatrel.DataAccess
{
    public class DataAccessModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<AppDbContext>()
                .WithParameter("options", DbContextOptionsFactory.Get())
                .InstancePerDependency();
        }
    }
}
