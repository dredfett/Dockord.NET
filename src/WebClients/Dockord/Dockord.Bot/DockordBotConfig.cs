﻿using Dockord.Bot.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Dockord.Bot.Services
{
    /// <summary>
    /// Creates a singleton that stores strongly typed values from <see cref="IConfiguration"/>.
    /// </summary>
    public sealed class DockordBotConfig : IDockordBotConfig
    {
        private static readonly DockordBotConfig _instance = new DockordBotConfig();
        private readonly IConfiguration _config;

        private DockordBotConfig()
        {
            _config = Create();

            _config.GetSection(nameof(BotSettings))
                   .Bind(BotSettings, x => x.BindNonPublicProperties = true);

            _config.GetSection(nameof(Serilog))
                   .Bind(Serilog, x => x.BindNonPublicProperties = true);
        }

        public SectionBotSettings BotSettings { get; } = new SectionBotSettings();
        public SectionSerilog Serilog { get; } = new SectionSerilog();

        public static DockordBotConfig Get()
        {
            return _instance;
        }

        public IConfigurationSection GetSerilogSection()
        {
            return _config.GetSection(nameof(Serilog));
        }

        public LogLevel GetMinimumLogLevel()
        {
            return (Serilog.MinimumLevel?.Default?.ToLower()) switch
            {
                "error" => LogLevel.Error,
                "warn" => LogLevel.Warning,
                "debug" => LogLevel.Debug,
                "information" => LogLevel.Information,
                "critical" => LogLevel.Critical,
                "trace" => LogLevel.Trace,
                _ => LogLevel.Error, // Default case
            };
        }

        /// <summary>
        /// Creates a project specific <see cref="IConfigurationBuilder"/>.
        /// </summary>
        /// <returns><see cref="IConfigurationBuilder"/></returns>
        private static IConfiguration Create()
        {
            string currentEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            var builder = new ConfigurationBuilder();

            var config = builder.SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{currentEnvironment}.json", optional: true)
                          .AddEnvironmentVariables()
                          .Build();

            if (config == null)
                throw new InvalidOperationException("Error building configuration.");

            return config;
        }
    }

    public interface IDockordBotConfig
    {
        SectionBotSettings BotSettings { get; }
        SectionSerilog Serilog { get; }

        LogLevel GetMinimumLogLevel();
        IConfigurationSection GetSerilogSection();
    }
}
