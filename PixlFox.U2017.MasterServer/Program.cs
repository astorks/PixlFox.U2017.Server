using Newtonsoft.Json;
using NLog;
using PixlFox.Gaming.GameServer;
using PixlFox.Gaming.GameServer.Commands;
using PixlFox.U2017.MasterServer.Components;
using PixlFox.U2017.MasterServer.Services;
using PixlFox.U2017.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PixlFox.U2017.MasterServer
{
    class Program
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        private static Core gameCore;
        private static HandlerRoutine consoleHandler;

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
        // A delegate type to be used as the handler routine for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);
        // An enumerated type for the control messages sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctrlType"></param>
        /// <returns></returns>
        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            // Put your own handler here
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    gameCore.Shutdown();
                    new Thread(new ThreadStart(() =>
                    {
                        while (!gameCore.IsFullyShutdown) Thread.Sleep(1000);
                    })).Start();
                    break;

                case CtrlTypes.CTRL_BREAK_EVENT:
                    gameCore.Shutdown();
                    new Thread(new ThreadStart(() =>
                    {
                        while (!gameCore.IsFullyShutdown) Thread.Sleep(1000);
                    })).Start();
                    break;

                case CtrlTypes.CTRL_CLOSE_EVENT:
                    gameCore.Shutdown();
                    new Thread(new ThreadStart(() =>
                    {
                        while (!gameCore.IsFullyShutdown) Thread.Sleep(1000);
                    })).Start();
                    break;

                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    gameCore.Shutdown();
                    new Thread(new ThreadStart(() =>
                    {
                        while (!gameCore.IsFullyShutdown) Thread.Sleep(1000);
                    })).Start();
                    break;
            }

            return true;
        }

        static void Main(string[] args)
        {
            ConfigureLogging();

            X509Certificate x509Certificate = Assembly.GetEntryAssembly().GetModules()[0].GetSignerCertificate();
            X509Certificate2 x509Certificate2 = x509Certificate != null ? new X509Certificate2(x509Certificate) : null;
            var signTool = new SignTool(x509Certificate2);

            if (x509Certificate2 == null || x509Certificate2.GetCertHashString() != "793D48CE7A9DDC71CE8A31E0929D215165FA9B8E")
            {
                logger.Fatal("Certificate is missing or could not be verified.");
                logger.Info("Certificate Hash: ", x509Certificate2?.GetCertHashString());
                Console.ReadLine();
                return;
            }

            consoleHandler = new HandlerRoutine(ConsoleCtrlCheck);
            SetConsoleCtrlHandler(consoleHandler, true);

            CreateServer();
            StartServer();

            gameCore.StartCommandInputThread(true);
        }

        private static void CreateServer()
        {
            gameCore = new Core("PixlFox U2017 Master Server", 32, allowCommandEngineClr: true);

            gameCore.RegisterService<ConfigService>();
            gameCore.RegisterService<AccountManagerService>();
            gameCore.RegisterService<WorldServerManagerService>();

            gameCore.RegisterComponent<InterCommunicationServer>();
            gameCore.RegisterComponent<PublicNetworkServer>();

            var configService = gameCore.GetService<ConfigService>();
            configService.Loaded += (s, e) =>
            {
                Steamworks.SteamworksWebApi.PublisherApiKey = configService.GetValue<string>("steamWorksApi:publisherApiKey");
                Steamworks.SteamworksWebApi.AppId = configService.GetValue<uint>("steamWorksApi:appId");
            };
            configService.Changed += (s, e) =>
            {
                Steamworks.SteamworksWebApi.PublisherApiKey = configService.GetValue<string>("steamWorksApi:publisherApiKey");
                Steamworks.SteamworksWebApi.AppId = configService.GetValue<uint>("steamWorksApi:appId");
            };

            gameCore.RegisterCommandHandler("clear", new Action(() => Console.Clear()), new CommandDescriptionInfo
            {
                Name = "clear",
                ReturnType = "Void",
                Description = "Clears all console output from the screen."
            });
            gameCore.RegisterCommandHandler("shutdown", new Action(() =>
            {
                gameCore.Shutdown();
                new Thread(new ThreadStart(() =>
                {
                    while (!gameCore.IsFullyShutdown) Thread.Sleep(1000);
                })).Start();
            }), new CommandDescriptionInfo
            {
                Name = "shutdown",
                ReturnType = "Void",
                Description = "Starts the shutdown procedure."
            });
            gameCore.RegisterCommandHandler("restart", new Action(() =>
            {
                new Thread(new ThreadStart(() =>
                {
                    gameCore.Shutdown();

                    while (!gameCore.IsFullyShutdown) Thread.Sleep(1000);

                    CreateServer();
                    StartServer();
                    gameCore.StartCommandInputThread(true);
                })).Start();
            }), new CommandDescriptionInfo
            {
                Name = "shutdown",
                ReturnType = "Void",
                Description = "Starts the shutdown procedure, then re-initalizes the server."
            });
        }

        private static void StartServer()
        {
            gameCore.Start();
        }

        private static void ConfigureLogging()
        {
            var logConfig = new NLog.Config.LoggingConfiguration();

            var consoleTarget = new NLog.Targets.ColoredConsoleTarget()
            {
                Layout = "[${level:uppercase=true}][${logger}] ${message}"
            };
            logConfig.AddTarget("console", consoleTarget);

            var logRule1 = new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, consoleTarget);
            logConfig.LoggingRules.Add(logRule1);

            NLog.LogManager.Configuration = logConfig;
        }
    }
}
