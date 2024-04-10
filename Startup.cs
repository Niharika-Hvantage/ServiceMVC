using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRES.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.EntityFrameworkCore;
using System.Security;
using CRES.DataContract;
using Newtonsoft.Json.Serialization;
using CRES.BusinessLogic;
using Microsoft.Extensions.Hosting;

namespace CRES.ServiceMVC
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

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //services.Configure<AppSettingsModel>(Configuration.GetSection("ConnectionStrings"));
            //services.AddOptions();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {

                        //builder.WithOrigins("http://localhost:58023",
                        //                    "http://www.contoso.com");

                        builder.AllowAnyOrigin()
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();
                    });

                options.AddPolicy("CRESPolicy",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();
                    });

            });

            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
            //    .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
    .AddNewtonsoftJson(options => { options.SerializerSettings.ContractResolver = new DefaultContractResolver(); });

            services.AddScoped<IEmailNotification, EmailNotification>();
         services.AddSwaggerGen();
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
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseRouting();
            app.UseCors();

            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller=Home}/{action=Index}/{id?}");
            //});

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
             app.UseSwagger();
              app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Web API V1"));
            //app.UseSwagger(c =>
            //{
            //    c.RouteTemplate = "CRES/Swagger/{documentName}/Swagger.json";
            //});
            //app.UseSwaggerUI(c=>
            //    {
            //        c.SwaggerEndpoint("/CRES/Swagger/v1/Swagger.json","Testservice");
            //        c.RoutePrefix = "CRES/Swagger";
            //});



        }
    }
}
