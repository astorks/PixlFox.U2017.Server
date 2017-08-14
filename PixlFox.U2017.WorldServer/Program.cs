﻿using Newtonsoft.Json;
using NLog;
using PixlFox.Gaming.GameServer;
using PixlFox.Gaming.GameServer.Commands;
using PixlFox.U2017.Tools;
using PixlFox.U2017.WorldServer.Components;
using PixlFox.U2017.WorldServer.Services;
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

namespace PixlFox.U2017.WorldServer
{
    class Program
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        private static HandlerRoutine consoleHandler;

        public static Core GameCore { get; private set; }

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
                    GameCore.Shutdown();
                    new Thread(new ThreadStart(() =>
                    {
                        while (!GameCore.IsFullyShutdown) Thread.Sleep(1000);
                    })).Start();
                    break;

                case CtrlTypes.CTRL_BREAK_EVENT:
                    GameCore.Shutdown();
                    new Thread(new ThreadStart(() =>
                    {
                        while (!GameCore.IsFullyShutdown) Thread.Sleep(1000);
                    })).Start();
                    break;

                case CtrlTypes.CTRL_CLOSE_EVENT:
                    GameCore.Shutdown();
                    new Thread(new ThreadStart(() =>
                    {
                        while (!GameCore.IsFullyShutdown) Thread.Sleep(1000);
                    })).Start();
                    break;

                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    GameCore.Shutdown();
                    new Thread(new ThreadStart(() =>
                    {
                        while (!GameCore.IsFullyShutdown) Thread.Sleep(1000);
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

            if(x509Certificate2 == null || x509Certificate2.GetCertHashString() != "793D48CE7A9DDC71CE8A31E0929D215165FA9B8E")
            {
                logger.Fatal("Certificate is missing or could not be verified.");
                logger.Info("Certificate Hash: ", x509Certificate2?.GetCertHashString());
                Console.ReadLine();
                return;
            }
            else if (!signTool.VerifyFileSignatures("../data/items.json", "../data/movers.json", "../data/worlds.json"))
            {
                logger.Fatal("Resource signatures could not be verified.");
                Console.ReadLine();
                return;
            }

            consoleHandler = new HandlerRoutine(ConsoleCtrlCheck);
            SetConsoleCtrlHandler(consoleHandler, true);

            CreateServer();
            StartServer();

            GameCore.StartCommandInputThread(true);
        }

        private static void CreateServer()
        {
            GameCore = new Core("PixlFox U2017 World Server", 32, allowCommandEngineClr: true);

            GameCore.RegisterService<ConfigService>();
            GameCore.RegisterService<DatabaseService>();
            GameCore.RegisterService<ResourceManager>();
            GameCore.RegisterComponent<PlayerManager>();
            GameCore.RegisterComponent<MoverManager>();

            GameCore.RegisterService<ChatService>();

            GameCore.RegisterComponent<MasterServerConnection>();
            GameCore.RegisterComponent<NetworkingComponent>();
            GameCore.RegisterComponent<WorldManager>();

            GameCore.RegisterCommandHandler("clear", new Action(() => Console.Clear()), new CommandDescriptionInfo
            {
                Name = "clear",
                ReturnType = "Void",
                Description = "Clears all console output from the screen."
            });
            GameCore.RegisterCommandHandler("shutdown", new Action(() =>
            {
                GameCore.Shutdown();
                new Thread(new ThreadStart(() =>
                {
                    while (!GameCore.IsFullyShutdown) Thread.Sleep(1000);
                })).Start();
            }), new CommandDescriptionInfo
            {
                Name = "shutdown",
                ReturnType = "Void",
                Description = "Starts the shutdown procedure."
            });
            GameCore.RegisterCommandHandler("restart", new Action(() =>
            {
                new Thread(new ThreadStart(() =>
                {
                    GameCore.Shutdown();

                    while (!GameCore.IsFullyShutdown) Thread.Sleep(1000);

                    CreateServer();
                    StartServer();
                    GameCore.StartCommandInputThread(true);
                })).Start();
            }), new CommandDescriptionInfo
            {
                Name = "restart",
                ReturnType = "Void",
                Description = "Starts the shutdown procedure, then re-initalizes the server."
            });
        }

        private static void StartServer()
        {
            GameCore.Start();
        }

        private static void ConfigureLogging()
        {
            var logConfig = new NLog.Config.LoggingConfiguration();

            var consoleTarget = new NLog.Targets.ColoredConsoleTarget()
            {
                Layout = "[${level:uppercase=true}][${logger}] ${message}"
            };
            logConfig.AddTarget("console", consoleTarget);

            var logRule1 = new NLog.Config.LoggingRule("*", LogLevel.Trace, consoleTarget);
            logConfig.LoggingRules.Add(logRule1);

            NLog.LogManager.Configuration = logConfig;
        }
    }
}
