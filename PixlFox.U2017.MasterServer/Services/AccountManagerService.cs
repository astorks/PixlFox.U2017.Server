using PixlFox.Gaming.GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PixlFox.Gaming.GameServer;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using PixlFox.Gaming.GameServer.Commands;
using System.ComponentModel;

namespace PixlFox.U2017.MasterServer.Services
{
    class AccountManagerService : IGameService
    {
        public ConcurrentDictionary<string, AccountInfo> CurrentAccounts { get; } = new ConcurrentDictionary<string, AccountInfo>();

        private RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

        public void Initialize(Core gameCore) { }
        public void Shutdown() { }

        public AccountInfo GetAccount(string authKey)
        {
            if (CurrentAccounts.ContainsKey(authKey))
                return CurrentAccounts[authKey];
            else
                return null;
        }

        public string AddAccount(ulong steamId)
        {
            var authKey = NewAuthKey();

            CurrentAccounts.TryAdd(authKey, new AccountInfo
            {
                AuthKey = authKey,
                SteamId = steamId
            });

            return authKey;
        }

        public void RemoveAccount(string authKey)
        {
            CurrentAccounts.TryRemove(authKey, out AccountInfo accountInfo);
        }

        private string NewAuthKey()
        {
            byte[] authKeyData = new byte[512];
            rngCsp.GetBytes(authKeyData);
            return Convert.ToBase64String(authKeyData);
        }

        [RegisteredCommand("requestPlayerGameBan")]
        [Description("Requests a player game ban on steam.")]
        public async void RequestPlayerGameBan(ulong steamId, uint duration)
        {
            var reportResponse = await Steamworks.SteamworksWebApi.ICheatReportingService.ReportPlayerCheating(steamId);
            var banResponse = await Steamworks.SteamworksWebApi.ICheatReportingService.RequestPlayerGameBan(steamId, reportResponse.ReportId.Value, "Internal ban", duration);
            Console.Write(banResponse);
        }

        [RegisteredCommand("removePlayerGameBan")]
        [Description("Removes an account game ban on steam.")]
        public async void RemovePlayerGameBan(ulong steamId)
        {
            var unbanResponse = await Steamworks.SteamworksWebApi.ICheatReportingService.RemovePlayerGameBan(steamId);
        }
    }

    public class AccountInfo
    {
        public string AuthKey { get; set; }
        public ulong SteamId { get; set; }
    }
}
