using System;
using Galashow.Bridge;
using Galashow.Bridge.Model;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace Galashow.Controller
{
    public class SampleManager : PersistentMonoSingleton<SampleManager>,ISamplePort
    {
        private SampleHandler _sample;

        void OnEnable()
        {
            _sample = BridgeManager.Instance.GetHandler<SampleHandler>("SampleHandler");
            if (_sample != null) _sample.AddPort(this);
        }

        void OnDisable()
        {
            if (_sample != null) _sample.RemovePort(this);
        }

        public void SendChangeBorderColor(string color)
        {
            if (_sample == null) { Debug.LogWarning("[SampleManager] SampleHandler is null"); return; }
            if (string.IsNullOrWhiteSpace(color)) { Debug.LogWarning("[SampleManager] color is empty"); return; }

            _sample.ChangeBorderColor(color);
            Debug.Log($"[SampleManager] NTY changeBorderColor sent: {color}");
        }

        public void SendCalculateMultiply(int a, int b, Action<int> onSuccess,Action onFail)
        {
            if (_sample == null) { Debug.LogWarning("[SampleManager] SampleHandler is null"); return; }

            _sample.CalculateMultiply(a, b,OnSuccess,OnError,OnTimeout);
            void OnSuccess(Acknowledge.R2U.CalculateMultiply data)
            {
                try
                {
                    onSuccess?.Invoke(data.Result);
                    Debug.Log($"[SampleManager] ACK CalculateAdd result={data.Result}");
                }
                catch (Exception e)
                {
                    onFail?.Invoke();
                    Debug.LogError($"[SampleManager] Parse ACK error: {e.Message}");
                }
            }
            void OnError(string err)
            {
                Debug.LogError($"[SampleManager] REQ CalculateAdd error: {err}");
            }
            void OnTimeout()
            {
                Debug.LogError("[SampleManager] REQ CalculateAdd timeout");
            }
        }
        public void OnChangeSphereColor(Notify.R2U.ChangeSphereColor data)
        {
            GameObject sphere = GameObject.Find("Sphere");
            if (sphere == null) return;

            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer == null) return;

            Color newColor;
            if (ColorUtility.TryParseHtmlString(data.Color, out newColor))
            {
                renderer.material.color = newColor;
            }
            else
            {
                Debug.LogWarning($"색상 변환 실패: {data.Color}");
            }
        }

        public void OnCalculateAdd(Bridge.Model.Request.R2U.CalculateAdd data, Action<Acknowledge.U2R.CalculateAdd> reply, Action<string> fail)
        {
            try
            {
                if (data == null)
                {
                    fail?.Invoke("데이터가 null입니다.");
                    return;
                }
                
                var response = new Acknowledge.U2R.CalculateAdd
                {
                    Result = data.a + data.b
                };
                
                reply?.Invoke(response);
                Debug.Log($"[SampleManager] CalculateAdd: {data.a} + {data.b} = {data.a + data.b}");
            }
            catch (Exception ex)
            {
                fail?.Invoke($"계산 중 오류가 발생했습니다: {ex.Message}");
                Debug.LogError($"[SampleManager] CalculateAdd error: {ex.Message}");
            }
        }

    }
}