using Newtonsoft.Json;

namespace Galashow.Bridge.Model
{
    public class Acknowledge
    {
        #region R2U

        public class R2U
        {
            //R2U_SampleManager_CalculateMultiply_ACK
            public class CalculateMultiply
            {
                [JsonProperty("result")]
                public int Result { get; set; }
            }
        }
        #endregion

        #region U2R

        public class U2R
        {
            //U2R_SampleManager_CalculateAdd_ACK
            public class CalculateAdd
            {
                [JsonProperty("result")]
                public int Result { get; set; }
            }
        }
        
        #endregion
    }
}