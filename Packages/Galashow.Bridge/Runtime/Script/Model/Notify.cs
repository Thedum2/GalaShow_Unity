using Newtonsoft.Json;

namespace Galashow.Bridge.Model
{
    public class Notify
    {
        #region R2U

        public class R2U
        {
            //R2U_SampleManager_ChangeSphereColor_NTY
            public class ChangeSphereColor
            {
                [JsonProperty("color")]
                public string Color { get; set; }
            }
        }
        #endregion

        #region U2R

        public class U2R
        {
            //U2R_SampleManager_ChangeBorderColor_NTY
            public class ChangeBorderColor
            {
                [JsonProperty("color")]
                public string Color { get; set; }

                public ChangeBorderColor(string color)
                {
                    Color = color;
                }
            }
        }
        
        #endregion
    }
}