using System.Collections.Generic;
using Newtonsoft.Json;

namespace Galashow.Bridge.Model
{
    public class Notify
    {
        #region R2U

        public class R2U
        {
            //R2U_SimulationManager_Selected_NTY
            public class Selected
            {
                [JsonProperty("index")] 
                public int Index { get; set; }
                
                [JsonProperty("user")] 
                public List<UserData> User { get; set; }
                
                [JsonProperty("title")] 
                public string Title { get; set; }
                
                [JsonProperty("choices")] 
                public List<Choice> Choices { get; set; }
                public class Choice
                {
                    [JsonProperty("choiceId")] 
                    public int ChoiceId { get; set; }
                    
                    [JsonProperty("text")] 
                    public string Text { get; set; }
                    
                    [JsonProperty("imageUrl")] 
                    public string ImageUrl { get; set; }
                }
                public class UserData
                {
                    [JsonProperty("id")] 
                    public int ID { get; set; }
                    
                    [JsonProperty("nickname")] 
                    public string Nickname { get; set; }
                }
            }

            //R2U_SimulationManager_SelectEvent_NTY
            public class SelectEvent
            {
                [JsonProperty("userIndex")] 
                public int UserIndex { get; set; }
                
                [JsonProperty("select")] 
                public int Select { get; set; }
            }
        }
        #endregion

        #region U2R

        public class U2R
        {
            //U2R_GameManager_LoadingProgress_NTY
            public class LoadingProgress
            {
                [JsonProperty("progress")] 
                public float Progress { get; set; }
                
                [JsonProperty("currentTask")] 
                public string CurrentTask { get; set; }

                public LoadingProgress(float progress, string currentTask)
                {
                    Progress = progress;
                    CurrentTask = currentTask;
                }
            }

            //U2R_SimulationManager_PostEnded_NTY
            public class PostEnded
            {
                [JsonProperty("resultChoiceId")]
                public string ResultChoiceId { get; set; }

                public PostEnded(string resultChoiceId)
                {
                    ResultChoiceId = resultChoiceId;
                }
            }
        }
        
        #endregion
    }
}