using System.Collections.Generic;
using Newtonsoft.Json;

namespace Galashow.Bridge.Model
{
    public class Notify
    {
        #region R2U

        public class R2U
        {
            //R2U_RGFManager_ChatInput_NTY
            public class RGFChatInput
            {
                [JsonProperty("roundNumber")]
                public int RoundNumber { get; set; }

                [JsonProperty("inputEventTime")]
                public long InputEventTime { get; set; }

                [JsonProperty("inputIdx")]
                public int InputIdx { get; set; }

                [JsonProperty("chatInfo")]
                public List<ChatInfo> ChatInfo { get; set; }
            }

            public class ChatInfo
            {
                [JsonProperty("playerIdx")]
                public int PlayerIdx { get; set; }

                [JsonProperty("message")]
                public string Message { get; set; }
            }
        }
        #endregion

        #region U2R

        public class U2R
        {
            //U2R_RGFManager_InitializeProgress_NTY
            public class RGFInitializeProgress
            {
                [JsonProperty("currentProgress")]
                public int CurrentProgress { get; set; }

                public RGFInitializeProgress(int currentProgress)
                {
                    CurrentProgress = currentProgress;
                }
            }

            //U2R_RGFManager_PhaseChanged_NTY
            public class RGFPhaseChanged
            {
                [JsonProperty("roundNumber")]
                public int RoundNumber { get; set; }

                [JsonProperty("miniGamePluginIdx")]
                public string MiniGamePluginIdx { get; set; }

                [JsonProperty("fromPhase")]
                public string FromPhase { get; set; }

                [JsonProperty("toPhase")]
                public string ToPhase { get; set; }

                public RGFPhaseChanged(int roundNumber, string miniGamePluginIdx, string fromPhase, string toPhase)
                {
                    RoundNumber = roundNumber;
                    MiniGamePluginIdx = miniGamePluginIdx;
                    FromPhase = fromPhase;
                    ToPhase = toPhase;
                }
            }

            //U2R_RGFManager_PhaseStarted_NTY
            public class RGFPhaseStarted
            {
                [JsonProperty("phase")]
                public string Phase { get; set; }

                [JsonProperty("duration")]
                public float Duration { get; set; }

                public RGFPhaseStarted(string phase, float duration)
                {
                    Phase = phase;
                    Duration = duration;
                }
            }

            //U2R_RGFManager_PhaseEnded_NTY
            public class RGFPhaseEnded
            {
                [JsonProperty("phase")]
                public string Phase { get; set; }

                [JsonProperty("duration")]
                public float Duration { get; set; }

                public RGFPhaseEnded(string phase, float duration)
                {
                    Phase = phase;
                    Duration = duration;
                }
            }

            //U2R_RGFManager_RoundStarted_NTY
            public class RGFRoundStarted
            {
                [JsonProperty("roundNumber")]
                public int RoundNumber { get; set; }

                [JsonProperty("miniGamePluginIdx")]
                public string MiniGamePluginIdx { get; set; }

                [JsonProperty("gameName")]
                public string GameName { get; set; }

                public RGFRoundStarted(int roundNumber, string miniGamePluginIdx, string gameName)
                {
                    RoundNumber = roundNumber;
                    MiniGamePluginIdx = miniGamePluginIdx;
                    GameName = gameName;
                }
            }

            //U2R_RGFManager_RoundCompleted_NTY
            public class RGFRoundCompleted
            {
                [JsonProperty("roundNumber")]
                public int RoundNumber { get; set; }

                [JsonProperty("miniGamePluginIdx")]
                public string MiniGamePluginIdx { get; set; }

                [JsonProperty("gameName")]
                public string GameName { get; set; }

                [JsonProperty("result")]
                public RoundResult Result { get; set; }

                public RGFRoundCompleted(int roundNumber, string miniGamePluginIdx, string gameName, RoundResult result)
                {
                    RoundNumber = roundNumber;
                    MiniGamePluginIdx = miniGamePluginIdx;
                    GameName = gameName;
                    Result = result;
                }
            }

            public class RoundResult
            {
                [JsonProperty("survivorsUserIdx")]
                public List<int> SurvivorsUserIdx { get; set; }

                [JsonProperty("eliminatedUserIdx")]
                public List<int> EliminatedUserIdx { get; set; }

                [JsonProperty("totalParticipants")]
                public int TotalParticipants { get; set; }

                [JsonProperty("remainingPlayers")]
                public int RemainingPlayers { get; set; }

                [JsonProperty("totalPlayTime")]
                public float TotalPlayTime { get; set; }
            }
        }
        
        #endregion
    }
}