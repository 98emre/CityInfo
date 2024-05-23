
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using CityInfo.DbContexts;
using CityInfo.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Security.Cryptography;

namespace CityInfo
{
    public class Program
    {
        public static void Main(string[] args)
        {

            // Create Log file
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/cityinfo.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            //builder.Logging.ClearProviders();
            //builder.Logging.AddConsole();

            // Serilog shows up
            builder.Host.UseSerilog();


            builder.Services.AddControllers(options =>
            {
                options.ReturnHttpNotAcceptable = true;
            }).AddNewtonsoftJson()
              .AddXmlDataContractSerializerFormatters();

            // Show problem during exception 
            builder.Services.AddProblemDetails();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSingleton<FileExtensionContentTypeProvider>();

            #if DEBUG
            builder.Services.AddTransient<IMailService,LocalMailService>();

#else
            builder.Services.AddTransient<IMailService,CloudMailService>();
#endif
            builder.Services.AddSingleton<CitiesDataStore>();

            builder.Services.AddDbContext<CityInfoContext>(options => options.UseSqlite(
                builder.Configuration["ConnectionStrings:CityInfoDBConnectionString"]));

            builder.Services.AddScoped<ICityInfoRepository, CityRepository>();

            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            builder.Services.AddAuthentication("Bearer")
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Authentication:Issuer"],
                        ValidAudience = builder.Configuration["Authentication:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(builder.Configuration["Authentication:SecretForKey"]))
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("MustBeFromParis", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("city", "Paris");
                });
            });

            builder.Services.AddApiVersioning(setupAction =>
            {
                setupAction.ReportApiVersions = true;
                setupAction.AssumeDefaultVersionWhenUnspecified = true;
                setupAction.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            }).AddMvc()
            .AddApiExplorer(setUpAction =>
            {
                setUpAction.SubstituteApiVersionInUrl = true;
            });

            var apiVersionDescriptionProvider = builder.Services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

            builder.Services.AddSwaggerGen(setUpAction =>
            {
                foreach (var desciption in apiVersionDescriptionProvider.ApiVersionDescriptions)
                {
                    setUpAction.SwaggerDoc(
                        $"{desciption.GroupName}",
                        new()
                        {
                            Title = "City Info API",
                            Version = desciption.ApiVersion.ToString(),
                            Description = "Through this API you can access citites and their points of interest"
                        });
                }

                var xmlCommentsFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlCommentsFullPath = Path.Combine(AppContext.BaseDirectory, xmlCommentsFile);

                setUpAction.IncludeXmlComments(xmlCommentsFullPath);

                setUpAction.AddSecurityDefinition("CityInfoApiBearerAuth", new()
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    Description = "Input a valid token to acces this API"
                });

                setUpAction.AddSecurityRequirement(new()
                {
                    {
                        new()
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "CityInfoApiBearerAuth"
                            }
                        },
                        new List<string>()
                    }
                });
            });

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler();
            }

            app.UseForwardedHeaders();

            //if (app.Environment.IsDevelopment())
            //{
                app.UseSwagger();
                app.UseSwaggerUI(setUpAction =>
                {
                    var descriptions = app.DescribeApiVersions();

                    foreach(var description in descriptions)
                    {
                        setUpAction.SwaggerEndpoint(
                            $"/swagger/{description.GroupName}/swagger.json",
                            description.GroupName.ToUpperInvariant());
                    }
                });
            //}

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.Run();
        }
    }
}
