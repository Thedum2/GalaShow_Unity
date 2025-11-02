using Newtonsoft.Json;

namespace Galashow.Bridge.Model
{
    public class Request
    {
        #region R2U

        public class R2U
        {
            //R2U_RGFManager_Initialize_REQ
            public class RGFInitialize
            {
                [JsonProperty("sessionId")]
                public string SessionId { get; set; }

                [JsonProperty("playerInfo")]
                public System.Collections.Generic.List<PlayerInfo> PlayerInfo { get; set; }

                [JsonProperty("config")]
                public Config Config { get; set; }
            }

            public class PlayerInfo
            {
                [JsonProperty("playerIdx")]
                public int PlayerIdx { get; set; }

                [JsonProperty("playerType")]
                public string PlayerType { get; set; }

                [JsonProperty("playerName")]
                public string PlayerName { get; set; }
            }

            public class Config
            {
                [JsonProperty("enableDebugLog")]
                public bool EnableDebugLog { get; set; }
            }

            //R2U_RGFManager_RegisterPlugin_REQ
            public class RGFRegisterPlugin
            {
                [JsonProperty("miniGameIdx")]
                public int MiniGameIdx { get; set; }

                [JsonProperty("miniGameName")]
                public string MiniGameName { get; set; }
            }

            //R2U_RGFManager_StartRound_REQ
            public class RGFStartRound
            {
                [JsonProperty("miniGamePluginIdx")]
                public string MiniGamePluginIdx { get; set; }

                [JsonProperty("roundNumber")]
                public int RoundNumber { get; set; }

                [JsonProperty("gameData")]
                public Newtonsoft.Json.Linq.JArray GameData { get; set; }

                [JsonProperty("phaseDuration")]
                public PhaseDuration PhaseDuration { get; set; }
            }

            public class PhaseDuration
            {
                [JsonProperty("READY")]
                public int Ready { get; set; }

                [JsonProperty("SETUP")]
                public int Setup { get; set; }

                [JsonProperty("PRESENT")]
                public int Present { get; set; }

                [JsonProperty("INPUT")]
                public int Input { get; set; }

                [JsonProperty("WAIT")]
                public int Wait { get; set; }

                [JsonProperty("EXECUTE")]
                public int Execute { get; set; }

                [JsonProperty("REVEAL")]
                public int Reveal { get; set; }

                [JsonProperty("CLEANUP")]
                public int Cleanup { get; set; }
            }

            //R2U_RGFManager_AbortRound_REQ
            public class RGFAbortRound
            {
                [JsonProperty("roundNumber")]
                public int RoundNumber { get; set; }
            }
        }

        #endregion

        #region U2R

        public class U2R
        {
        }

        #endregion
    }
}