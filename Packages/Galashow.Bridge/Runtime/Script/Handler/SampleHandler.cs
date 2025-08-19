using System;
using System.Collections.Generic;
using Galashow.Bridge.Model;
using Newtonsoft.Json;

namespace Galashow.Bridge
{
    public sealed class SampleHandler : BaseMessageHandler
    {
        private readonly List<ISamplePort> _ports = new();

        public SampleHandler() : base("SampleHandler") {}

        public void AddPort(ISamplePort port)
        {
            if (port != null && !_ports.Contains(port)) _ports.Add(port);
        }

        public void RemovePort(ISamplePort port) => _ports.Remove(port);

        #region =========U2R=========

        public void ChangeBorderColor(string color)
        {
            var data = new Notify.U2R.ChangeBorderColor(color);
            NTY("ChangeBorderColor", data);
        }

        public void CalculateMultiply(
            int a, int b,
            Action<Acknowledge.R2U.CalculateMultiply> onSuccess = null,
            Action<string> onError = null,
            Action onTimeout = null,
            float timeoutSeconds = -1f)
        {
            var data = new Request.U2R.CalculateMultiply(a, b);
            REQ("CalculateMultiply", data, OnSuccess, onError, onTimeout, timeoutSeconds);
            void OnSuccess(Message message)
            {
                try
                {
                    if (message.data != null)
                    {
                        var response = JsonConvert.DeserializeObject<Acknowledge.R2U.CalculateMultiply>(message.data.ToString());
                        onSuccess?.Invoke(response);
                    }
                    else
                    {
                        onError?.Invoke("응답 데이터가 비어있습니다.");
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"응답 파싱 중 오류 발생: {ex.Message}");
                }
            }
        }

        #endregion

        #region =========R2U=========
        
        public override void HandleNotify(Message message)
        {
            var (_, action) = Util.ParseRoute(message.route);
            switch (action)
            {
                case "ChangeSphereColor":
                    if (Util.TryTo<Notify.R2U.ChangeSphereColor>(message.data, out var dto, out var err))
                    {
                        foreach (var p in _ports) p.OnChangeSphereColor(dto);
                    }
                    break;

                default:
                    Util.LogWarning($"[SampleHandler] Unknown NTY action '{action}'");
                    break;
            }
        }

        public override void HandleRequest(Message message, Action<object> onSuccess, Action<string> onError)
        {
            var (_, action) = Util.ParseRoute(message.route);
            switch (action)
            {
                case "CalculateAdd":
                    if (!Util.TryTo<Request.R2U.CalculateAdd>(message.data, out var req, out var parseErr))
                    {
                        onError?.Invoke($"bad payload: {parseErr}");
                        return;
                    }
                    if (_ports.Count == 0)
                    {
                        onError?.Invoke("No ISamplePort registered");
                        return;
                    }

                    bool replied = false;
                    void Reply(Acknowledge.U2R.CalculateAdd resp)
                    {
                        if (replied) { Util.LogWarning("[SampleHandler] duplicate reply ignored"); return; }
                        replied = true;
                        onSuccess?.Invoke(resp);
                    }
                    void Fail(string e)
                    {
                        if (replied) return;
                        replied = true;
                        onError?.Invoke(e);
                    }

                    foreach (var p in _ports) p.OnCalculateAdd(req, Reply, Fail);
                    break;

                default:
                    onError?.Invoke($"Unknown REQ action '{action}'");
                    break;
            }
        }
        
        #endregion
    }
}