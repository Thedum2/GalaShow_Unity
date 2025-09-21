using Newtonsoft.Json;

namespace Galashow.Bridge.Model
{
    public class Request
    {
        #region R2U

        public class R2U
        {
            //R2U_GameManager_Initialize_REQ
            public class Initialize
            {
                [JsonProperty("playerName")]
                public string PlayerName { get; set; }
                
                [JsonProperty("sessionId")]
                public string SessionId { get; set; }
            }
            
            //R2U_SimulationManager_PostStarted_REQ
            public class PostStarted
            {
                [JsonProperty("resultChoiceId")]
                public int ResultChoiceId { get; set; }
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