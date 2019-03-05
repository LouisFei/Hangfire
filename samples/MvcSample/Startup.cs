using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.Owin;
using MvcSample;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace MvcSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalConfiguration.Configuration
                //.UseSqlServerStorage(@"Server=.\sqlexpress;Database=Hangfire.Sample;Trusted_Connection=True;")
                .UseSqlServerStorage(@"server=.;database=Hangfire;uid=hf;pwd=hf123;")
                //.UseMsmqQueues(@".\Private$\hangfire{0}", "default", "critical")
                .UseDashboardMetric(SqlServerStorage.ActiveConnections)
                .UseDashboardMetric(SqlServerStorage.TotalConnections)
                .UseDashboardMetric(DashboardMetrics.FailedCount);
            
            app.UseHangfireDashboard();
        }
    }
}
