using Newtonsoft.Json;

namespace Galashow.Bridge.Model
{
    public class Request
    {
        #region R2U

        public class R2U
        {
            //R2U_SampleManager_CalculateAdd_REQ
            public class CalculateAdd
            {
                [JsonProperty("a")] public int a { get; set; }
                [JsonProperty("b")] public int b { get; set; }

                public CalculateAdd(int a, int b)
                {
                    this.a = a;
                    this.b = b;
                }
            }
        }
        #endregion

        #region U2R

        public class U2R
        {
            //U2R_SampleManager_CalculateMultiply_REQ
            public class CalculateMultiply
            {
                [JsonProperty("a")] public int a { get; set; }
                [JsonProperty("b")] public int b { get; set; }

                public CalculateMultiply(int a, int b)
                {
                    this.a = a;
                    this.b = b;
                }
            }
        }
        
        #endregion
    }
}