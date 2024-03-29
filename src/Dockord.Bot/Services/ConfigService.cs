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
    public sealed class ConfigService : IConfigService
    {
        private static IConfigService _instance;
        private readonly IConfiguration _config;
        private static readonly object _lockObject = new object();

        private ConfigService()
        {
            if (_instance != null)
                throw new InvalidOperationException("Use Get() method to get an instance of this class.");

            _config = Create();

            _config.GetSection(nameof(BotSettings))
                   .Bind(BotSettings, x => x.BindNonPublicProperties = true);

            _config.GetSection(nameof(Serilog))
                   .Bind(Serilog, x => x.BindNonPublicProperties = true);
        }

        public SectionBotSettings BotSettings { get; } = new SectionBotSettings();
        public SectionSerilog Serilog { get; } = new SectionSerilog();

        public static IConfigService Get()
        {
            if (_instance == null)
            {
                lock (_lockObject) // Create thread safe singleton
                {
                    _instance = new ConfigService();
                }
            }

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
        /// Creates a project specific <see cref="IConfiguration"/>.
        /// </summary>
        /// <returns><see cref="IConfiguration"/></returns>
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

    public interface IConfigService
    {
        SectionBotSettings BotSettings { get; }
        SectionSerilog Serilog { get; }

        LogLevel GetMinimumLogLevel();
        IConfigurationSection GetSerilogSection();
    }
}
