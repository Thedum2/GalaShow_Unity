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

                /// <summary>
                /// 참가자 캐릭터. Admin viewer_avatars.name (선택)
                /// </summary>
                [JsonProperty("avatarName")]
                public string AvatarName { get; set; }
            }

            public class Config
            {
                [JsonProperty("enableDebugLog")]
                public bool EnableDebugLog { get; set; }

                /// <summary>
                /// 로비에서 고른 시청자 아바타 이름 목록 (선택). avatarName이 없는 참가자에게 순서대로 나눠 준다.
                /// </summary>
                [JsonProperty("avatarNames")]
                public System.Collections.Generic.List<string> AvatarNames { get; set; }
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
                public Newtonsoft.Json.Linq.JToken GameData { get; set; }

                [JsonProperty("phaseDuration")]
                public PhaseDuration PhaseDuration { get; set; }

                /// <summary>
                /// 연습 라운드 (Tutorial). 판정·연출은 같지만 참가자 생존 상태를 바꾸지 않는다.
                /// </summary>
                [JsonProperty("practice")]
                public bool Practice { get; set; }
            }

            public class PhaseDuration
            {
                [JsonProperty("READY")]
                public float Ready { get; set; }

                [JsonProperty("SETUP")]
                public float Setup { get; set; }

                [JsonProperty("PRESENT")]
                public float Present { get; set; }

                [JsonProperty("INPUT")]
                public float Input { get; set; }

                [JsonProperty("WAIT")]
                public float Wait { get; set; }

                [JsonProperty("EXECUTE")]
                public float Execute { get; set; }

                [JsonProperty("REVEAL")]
                public float Reveal { get; set; }

                [JsonProperty("CLEANUP")]
                public float Cleanup { get; set; }
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