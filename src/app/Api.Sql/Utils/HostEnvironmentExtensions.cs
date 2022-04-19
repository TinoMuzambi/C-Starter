﻿using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Constants = Common.Constants;

namespace Api.Sql.Utils;

public static class HostEnvironmentExtensions
{
    public static Logger BuildLogger(this IHostEnvironment hostEnvironment) =>
        new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Filter.ByExcluding($"RequestPath = '{Constants.HealthcheckPath}' and StatusCode = 200")
            .WriteTo.Logger(lc =>
            {
                if (hostEnvironment.IsDevelopment())
                {
                    lc.WriteTo.Console();
                }
                else
                {
                    lc.WriteTo.Console(new RenderedCompactJsonFormatter());
                }
            })
            .CreateLogger();
}
