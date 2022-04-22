﻿using Api.Sql.Utils;
using Api.Views.Mappings;
using Api.Views.Validators.Account;
using Common;
using Common.DalSql;
using Common.Settings;
using Common.Utils;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var environment = builder.Environment;
var configuration = builder.Configuration;
var services = builder.Services;

builder.Host.UseSerilog();
Log.Logger = environment.BuildLogger();

var dbSettings = services.AddSettings<DbSettingsSql>(configuration, "DbSql");
var appSettings = services.AddSettings<AppSettings>(configuration, "App");
var cacheSettings = services.AddSettings<CacheSettings>(configuration, "Cache");
services.AddSettings<TokenExpirationSettings>(configuration, "TokenExpiration");
services.AddSettings<EmailSettings>(configuration, "Email");

services.AddDiConfiguration();
services.AddCache(cacheSettings);
services.AddCors(appSettings);
services.AddFeatureManagement();
services.AddApiControllers();
services.AddSwagger();
services.AddHttpContextAccessor();
services.AddAuthorization();
services.AddAutoMapper(typeof(UserProfile));
services.AddFluentValidation(config =>
    config.RegisterValidatorsFromAssemblyContaining(typeof(SignInModelValidator))
);
services.AddHealthChecks(cacheSettings);
services.InitializeDb(dbSettings);

var app = builder.Build();

if (environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint(Constants.Swagger.Url, Constants.Swagger.Name)
    );
    app.UseDeveloperExceptionPage();
}
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCors(Constants.CorsPolicy.AllowSpecificOrigin);
app.UseTokenAuthentication();
app.UseDbContextSaveChanges();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHealthChecks(Constants.HealthcheckPath, new HealthCheckOptions
    {
        AllowCachingResponses = false
    });
});

try
{
    Log.Information("Starting web host");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}