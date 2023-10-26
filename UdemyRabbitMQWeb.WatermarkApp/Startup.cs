using HealthChecks.UI.Client;
using HealthChecks.UI.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UdemyRabbitMQWeb.WatermarkApp.BackgroundServices;
using UdemyRabbitMQWeb.WatermarkApp.Models;
using UdemyRabbitMQWeb.WatermarkApp.Services;

namespace UdemyRabbitMQWeb.WatermarkApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitMqConnectionString = Configuration.GetConnectionString("RabbitMQ");
            services.AddSingleton(sp => new ConnectionFactory() { Uri = new Uri(rabbitMqConnectionString), DispatchConsumersAsync = true });
            services.AddSingleton<RabbitMQClientService>();
            services.AddSingleton<RabbitMQPublisher>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: "productDb");
            });
            services.AddHostedService<ImageWatermarkProcessBackgroundService>();
            services.AddControllersWithViews();
            services.AddHealthChecks()
                .AddRabbitMQ(
               rabbitMqConnectionString,
               name: "RabbitMQ",
               failureStatus: HealthStatus.Unhealthy | HealthStatus.Degraded,
               timeout: TimeSpan.FromSeconds(1),
               tags: new string[] { "services" });

            services.AddHealthChecksUI(setupSettings: settings =>
            {
                settings.AddWebhookNotification("Teams Notification WebHook", "https://hooks.slack.com/services/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",
                    "{\"text\": \"[[LIVENESS]] artýk ulaþýlamýyor : [[FAILURE]]\"}",
                    "{\"text\": \"[[LIVENESS]] kalktý þimdi iyi merak etme\"}");
            })
            .AddInMemoryStorage();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            //var options = new HealthCheckOptions();
            //options.ResponseWriter = async (c, r) =>
            //{
            //    c.Response.ContentType = "application/json";
            //    var result = JsonConvert.SerializeObject(new
            //    {
            //        status = r.Status.ToString(),
            //        errors = r.Entries.Select(e => new { key = e.Key, value = e.Value.Status.ToString() }),
            //        durations = r.TotalDuration.TotalMilliseconds.ToString()
            //    });
            //    await c.Response.WriteAsync(result);
            //};

            app.UseHealthChecks("/health", options: new HealthCheckOptions() { Predicate = _ => true, ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });

            app.UseEndpoints(endpoints =>
            {


                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHealthChecksUI();

                endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

            });



        }
    }
}
