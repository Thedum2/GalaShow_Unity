using System;
using System.Collections.Generic;
using Galashow.Bridge.Model;

namespace Galashow.Bridge
{
    public sealed class GameHandler : BaseMessageHandler
    {
        private readonly List<IGamePort> _ports = new();

        public GameHandler() : base("GameManager") {}

        public void AddPort(IGamePort port)
        {
            if (port != null && !_ports.Contains(port)) _ports.Add(port);
        }

        public void RemovePort(IGamePort port)
        {
            _ports.Remove(port);
        }

        #region =========U2R=========

        public void LoadingProgress(float progress, string currentTask)
        {
            NTY("LoadingProgress", new Notify.U2R.LoadingProgress(progress, currentTask));
        }

        #endregion

        #region =========R2U=========

        public override void HandleNotify(Message message)
        {
            var (_, action) = Util.ParseRoute(message.route);
            switch (action)
            {
                case "Ended":
                    _ports.ForEach(p => p.R2U_GameManager_Ended_NTY());
                    break;
                default:
                    Util.LogWarning($"[GameHandler] Unknown NTY action '{action}'");
                    break;
            }
        }

        public override void HandleRequest(Message message, Action<object> onSuccess, Action<string> onError)
        {
            var (_, action) = Util.ParseRoute(message.route);
            switch (action)
            {
                case "Initialize":
                {
                    if (!Util.TryTo<Request.R2U.Initialize>(message.data, out var req, out var err))
                    {
                        onError?.Invoke($"bad payload: {err}");
                        return;
                    }

                    if (_ports.Count == 0)
                    {
                        onError?.Invoke("No IGamePort registered");
                        return;
                    }

                    bool replied = false;
                    void Reply(Acknowledge.U2R.Initialize data)
                    {
                        if (replied) { Util.LogWarning($"[GameHandler] duplicate reply ignored"); return; }
                        replied = true;
                        onSuccess?.Invoke(data);
                    }
                    void Fail(string e)
                    {
                        if (replied) return;
                        replied = true;
                        onError?.Invoke(e);
                    }

                    _ports.ForEach(p => p.R2U_GameManager_Initialize_REQ(req, Reply, Fail));
                }
                    break;
                default:
                    onError?.Invoke($"[GameHandler] Unknown REQ action '{action}'");
                    break;
            }
        }

        #endregion
    }
}
