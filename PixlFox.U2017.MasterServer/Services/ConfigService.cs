﻿using PixlFox.Gaming.GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PixlFox.Gaming.GameServer;
using System.IO;
using System.Dynamic;
using PixlFox.Gaming.GameServer.Commands;
using Microsoft.Extensions.Configuration;
using NLog;

namespace PixlFox.U2017.MasterServer.Services
{
    class ConfigService : IGameService
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public IConfigurationRoot Config { get; private set; }
        public event EventHandler Loaded;
        public event EventHandler Changed;


        public void Initialize(Core gameCore)
        {
            var configBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());

            configBuilder.AddEnvironmentVariables("cfg:");

            if (Directory.Exists("./cfg"))
                foreach (var configJsonFile in Directory.GetFiles("./cfg", "*.json"))
                {
                    configBuilder.AddJsonFile(configJsonFile, true);
                    logger.Debug("Found and loaded config file {0}", configJsonFile.Replace('\\', '/'));
                }

            Config = configBuilder.Build();
            Config.GetReloadToken().RegisterChangeCallback(ConfigChanged, this);

            FileSystemWatcher fileWatcher = new FileSystemWatcher("./cfg", "*.json")
            {
                NotifyFilter = NotifyFilters.LastWrite,
            };
            fileWatcher.Changed += (s, e) =>
            {
                fileWatcher.EnableRaisingEvents = false;
                logger.Trace("Config file {0} changed.", e.FullPath.Replace('\\', '/'));
                if (e.ChangeType == WatcherChangeTypes.Changed) ReloadConfig();
                fileWatcher.EnableRaisingEvents = true;
            };
            fileWatcher.EnableRaisingEvents = true;

            Loaded?.Invoke(this, new EventArgs());
        }

        public void Shutdown()
        {
            
        }

        private void ConfigChanged(object state)
        {
            Steamworks.SteamworksWebApi.PublisherApiKey = GetValue<string>("steamWorksApi:publisherApiKey");
            Steamworks.SteamworksWebApi.AppId = GetValue<uint>("steamWorksApi:appId");

            this.Changed?.Invoke(state, new EventArgs());
            Config.GetReloadToken().RegisterChangeCallback(ConfigChanged, this);
        }

        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            return Config.GetValue(key, defaultValue);
        }

        [RegisteredCommand("getConfigValue")]
        public object GetValue(string key, Type t = null, object defaultValue = null)
        {
            if (t == null)
                t = typeof(String);

            return Config.GetValue(t, key, defaultValue);
        }

        public T GetSectionAs<T>(string key)
        {
            return Config.GetSection(key).Get<T>();
        }

        [RegisteredCommand("getConfigSection")]
        public Dictionary<string, object> GetSection(string key)
        {
            return Config.GetSection(key).Get<Dictionary<string, object>>();
        }

        [RegisteredCommand("reloadConfig")]
        public void ReloadConfig()
        {
            logger.Debug("Reloading config...");
            Config.Reload();
        }
    }
}