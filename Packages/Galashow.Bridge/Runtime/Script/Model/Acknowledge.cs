using Newtonsoft.Json;

namespace Galashow.Bridge.Model
{
    public class Acknowledge
    {
        #region R2U

        public class R2U
        {
            
        }
        #endregion

        #region U2R

        public class U2R
        {
            //U2R_RGFManager_Initialize_ACK
            public class RGFInitialize
            {
                [JsonProperty("initialized")]
                public bool Initialized { get; set; }

                [JsonProperty("version")]
                public string Version { get; set; }

                public RGFInitialize(bool initialized, string version)
                {
                    Initialized = initialized;
                    Version = version;
                }
            }

            //U2R_RGFManager_RegisterPlugin_ACK
            public class RGFRegisterPlugin
            {
                [JsonProperty("miniGameIdx")]
                public int MiniGameIdx { get; set; }

                [JsonProperty("miniGameName")]
                public string MiniGameName { get; set; }

                [JsonProperty("miniGamePluginIdx")]
                public string MiniGamePluginIdx { get; set; }

                public RGFRegisterPlugin(int miniGameIdx, string miniGameName, string miniGamePluginIdx)
                {
                    MiniGameIdx = miniGameIdx;
                    MiniGameName = miniGameName;
                    MiniGamePluginIdx = miniGamePluginIdx;
                }
            }

            //U2R_RGFManager_StartRound_ACK
            public class RGFStartRound
            {
                [JsonProperty("started")]
                public bool Started { get; set; }

                [JsonProperty("roundNumber")]
                public int RoundNumber { get; set; }

                public RGFStartRound(bool started, int roundNumber)
                {
                    Started = started;
                    RoundNumber = roundNumber;
                }
            }

            //U2R_RGFManager_AbortRound_ACK
            public class RGFAbortRound
            {
                [JsonProperty("aborted")]
                public bool Aborted { get; set; }

                [JsonProperty("roundNumber")]
                public int RoundNumber { get; set; }

                public RGFAbortRound(bool aborted, int roundNumber)
                {
                    Aborted = aborted;
                    RoundNumber = roundNumber;
                }
            }
        }
        
        #endregion
    }
}