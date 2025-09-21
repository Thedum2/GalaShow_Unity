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
            //U2R_GameManager_Initialize_ACK
            public class Initialize
            {
                [JsonProperty("gameVersion")]
                public string GameVersion { get; set; }
                
                [JsonProperty("maxPlayers")]
                public int MaxPlayers { get; set; }
            }
        }
        
        #endregion
    }
}