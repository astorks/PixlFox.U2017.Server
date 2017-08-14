using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pathoschild.Http.Client;

namespace Steamworks
{
    public static class SteamworksWebApi
    {
        public static string PublisherApiKey { get; set; }
        public static uint AppId { get; set; }

        private static FluentClient FluentClient { get; } = new FluentClient("https://partner.steam-api.com");

        public static class ISteamUserAuth
        {
            public static async Task<SteamworksWebApiResponse<Responses.AuthenticateUserTicketResponse>> AuthenticateUserTicket(byte[] ticket, uint ticketLen)
            {
                var ticketHex = "";
                for (int i = 0; i < ticketLen; i++)
                    ticketHex += String.Format("{0:X2}", ticket[i]);

                var responseData = await FluentClient.GetAsync("ISteamUserAuth/AuthenticateUserTicket/v1")
                    .WithArgument("key", PublisherApiKey)
                    .WithArgument("appid", AppId)
                    .WithArgument("ticket", ticketHex)
                    .As<SteamworksWebApiResponseData<Responses.AuthenticateUserTicketResponse>>();

                return responseData.Response;
            }
        }

        public static class ICheatReportingService
        {
            public static async Task<Responses.ReportPlayerCheatingResponse> ReportPlayerCheating(ulong steamId)
            {
                var responseData = await FluentClient.PostAsync("ICheatReportingService/ReportPlayerCheating/v1")
                    .WithArgument("key", PublisherApiKey)
                    .WithArgument("appid", AppId)
                    .WithArgument("steamid", steamId)
                    .As<SteamworksWebApiResponse2<Responses.ReportPlayerCheatingResponse>>();

                return responseData.Response;
            }

            public static async Task<Responses.RequestPlayerGameBanResponse> RequestPlayerGameBan(ulong steamId, ulong reportId, string description, uint duration, bool delayBan = false, uint flags = 0)
            {
                var responseData = await FluentClient.PostAsync("ICheatReportingService/RequestPlayerGameBan/v1")
                    .WithArgument("key", PublisherApiKey)
                    .WithArgument("appid", AppId)
                    .WithArgument("steamid", steamId)
                    .WithArgument("reportid", reportId)
                    .WithArgument("cheatdescription", description)
                    .WithArgument("duration", duration)
                    .WithArgument("delayban", delayBan)
                    .WithArgument("flags", flags)
                    .As<SteamworksWebApiResponse2<Responses.RequestPlayerGameBanResponse>>();

                return responseData.Response;
            }

            public static async Task<Responses.RemovePlayerGameBanResponse> RemovePlayerGameBan(ulong steamId)
            {
                var responseData = await FluentClient.PostAsync("ICheatReportingService/RequestPlayerGameBan/v1")
                    .WithArgument("key", PublisherApiKey)
                    .WithArgument("appid", AppId)
                    .WithArgument("steamid", steamId)
                    .As<SteamworksWebApiResponse2<Responses.RemovePlayerGameBanResponse>>();

                return responseData.Response;
            }
        }
    }

    public class SteamworksWebApiResponseData<T>
    {
        public SteamworksWebApiResponse<T> Response { get; set; }
    }

    public class SteamworksWebApiResponse2<T>
    {
        public T Response { get; set; }
    }

    public class SteamworksWebApiResponse<T>
    {
        public bool IsError
        { 
            get
            {
                return this.Error != null;
            }
        }

        [JsonProperty("error")]
        public SteamworksWebApiResponseError Error { get; set; }

        [JsonProperty("params")]
        public T Params { get; set; }
    }

    public class SteamworksWebApiResponseError
    {
        [JsonProperty("errorcode")]
        public int ErrorCode { get; set; }

        [JsonProperty("errordesc")]
        public string ErrorDescription { get; set; }
    }

    public static class Responses
    {
        public class ReportPlayerCheatingResponse
        {
            [JsonIgnore]
            public ulong? SteamId64
            {
                get
                {
                    if (ulong.TryParse(SteamId, out ulong steamId64))
                        return steamId64;
                    else
                        return null;
                }
            }

            [JsonIgnore]
            public ulong? ReportId
            {
                get
                {
                    if (ulong.TryParse(_ReportId, out ulong reportId))
                        return reportId;
                    else
                        return null;
                }
            }

            [JsonProperty("steamid")]
            public string SteamId { get; set; }

            [JsonProperty("reportid")]
            public string _ReportId { get; set; }

            [JsonProperty("banstarttime")]
            public int BanStartTime { get; set; }

            [JsonProperty("suspicionlevel")]
            public int SuspicionLevel { get; set; }
        }

        public class RequestPlayerGameBanResponse
        {
            [JsonIgnore]
            public ulong? SteamId64
            {
                get
                {
                    if (ulong.TryParse(SteamId, out ulong steamId64))
                        return steamId64;
                    else
                        return null;
                }
            }

            [JsonProperty("steamid")]
            public string SteamId { get; set; }
        }

        public class RemovePlayerGameBanResponse
        {
            [JsonIgnore]
            public ulong? SteamId64
            {
                get
                {
                    if (ulong.TryParse(SteamId, out ulong steamId64))
                        return steamId64;
                    else
                        return null;
                }
            }

            [JsonProperty("steamid")]
            public string SteamId { get; set; }
        }

        public class AuthenticateUserTicketResponse
        {
            [JsonIgnore]
            public bool IsAuthenticated
            {
                get
                {
                    return Result == "OK";
                }
            }

            [JsonIgnore]
            public ulong? SteamId64
            {
                get
                {
                    if (ulong.TryParse(SteamId, out ulong steamId64))
                        return steamId64;
                    else
                        return null;
                }
            }

            [JsonIgnore]
            public ulong? OwnerSteamId64
            {
                get
                {
                    if (ulong.TryParse(OwnerSteamId, out ulong steamId64))
                        return steamId64;
                    else
                        return null;
                }
            }

            [JsonProperty("result")]
            public string Result { get; set; }

            [JsonProperty("steamid")]
            public string SteamId { get; set; }

            [JsonProperty("ownersteamid")]
            public string OwnerSteamId { get; set; }

            [JsonProperty("vacbanned")]
            public bool VacBanned { get; set; }

            [JsonProperty("publisherbanned")]
            public bool PublisherBanned { get; set; }
        }
    }
}
