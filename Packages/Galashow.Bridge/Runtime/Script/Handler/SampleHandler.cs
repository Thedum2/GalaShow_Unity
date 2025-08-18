using Galashow.Bridge.Model;

namespace Galashow.Bridge
{
    public sealed class SampleHandler : BaseMessageHandler
    {
        public SampleHandler() : base("SampleHandler") {}

        //U2R_SampleManager_ChangeBorderColor_NTY
        public void ChangeBorderColor(string color)
        {
            Notify.U2R.ChangeBorderColor data = new Notify.U2R.ChangeBorderColor(color);
            NTY("changeBorderColor", data);
        }
        
        //U2R_SampleManager_CalculateMultiply_REQ
        public void CalculateMultiply(int a,int b, System.Action<Message> onSuccess = null,System.Action<string> onError = null,System.Action onTimeout = null, float timeoutSeconds = -1f)
        {
            Request.U2R.CalculateMultiply data = new Request.U2R.CalculateMultiply(a, b);
            REQ("login", data, onSuccess, onError, onTimeout, timeoutSeconds);
        }
        
        public override void HandleRequest(Message message, System.Action<object> onSuccess, System.Action<string> onError)
        {
            //TODO :: 여기서 각 Manager에 뿌려줘야 될듯
            onSuccess?.Invoke(new { ok = true });
        }
    }
}