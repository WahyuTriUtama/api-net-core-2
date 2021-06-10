using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Api.Helpers;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Api
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
			//cors
			services.AddCors();
			
			//default
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

			// configure strongly typed settings objects
			var appSettingsSection = Configuration.GetSection("AppSettings");
			services.Configure<AppSettings>(appSettingsSection);

			// configure jwt authentication
			var appSettings = appSettingsSection.Get<AppSettings>();
			var key = Encoding.ASCII.GetBytes(appSettings.Secret);
			services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.RequireHttpsMetadata = false;
				options.SaveToken = true;
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = false,
					ValidateAudience = false
				};
			});

			// Global Authorize. Use [AllowAnonymous] for those where you don't need it
			services.AddMvc(options =>
			{
				var policy = new AuthorizationPolicyBuilder()
				.RequireAuthenticatedUser()
				.Build();
				options.Filters.Add(new AuthorizeFilter(policy));
			});

			// configure DI for application services
			services.AddScoped<IUserService, UserService>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseHsts();
			}

			// global cors policy
			app.UseCors(x => x
				.AllowAnyOrigin()
				.AllowAnyMethod()
				.AllowAnyHeader());

			// Authentication
			app.UseAuthentication();

			// ExceptionHandler
			app.UseStatusCodePages(async context =>
			{
				var errorMsg = "";
				int errorCode = context.HttpContext.Response.StatusCode;
				if (errorCode == 500) {
					errorMsg = "Internal Server Error";
				} else if (errorCode == 400) {
					errorMsg = "Bad Request";
				} else if (errorCode == 401) {
					errorMsg = "Unauthorized";
				} else if (errorCode == 404) {
					errorMsg = "Page Not Found";
				}

				var result = JsonConvert.SerializeObject(
					new {
						status = new 				{
							code = errorCode,
							error = true,
							message = errorMsg
						}
				});
				context.HttpContext.Response.ContentType = "application/json";

				await context.HttpContext.Response.WriteAsync(result);
			});

			//default
			app.UseHttpsRedirection();
			app.UseMvc();
		}
	}
}
