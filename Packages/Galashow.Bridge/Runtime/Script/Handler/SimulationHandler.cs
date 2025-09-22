using System;
using System.Collections.Generic;
using Galashow.Bridge.Model;
using Newtonsoft.Json;

namespace Galashow.Bridge
{
    public sealed class SimulationHandler : BaseMessageHandler
    {
        private readonly List<ISimulationPort> _ports = new();

        public SimulationHandler() : base("SimulationManager") {}

        public void AddPort(ISimulationPort port)
        {
            if (port != null && !_ports.Contains(port)) _ports.Add(port);
        }

        public void RemovePort(ISimulationPort port)
        {
            _ports.Remove(port);
        }

        #region =========U2R=========

        
        #endregion

        #region =========R2U=========

        public override void HandleNotify(Message message)
        {
            var (_, action) = Util.ParseRoute(message.route);
            switch (action)
            {
                case "Selected":
                    if (Util.TryTo<Notify.R2U.Selected>(message.data, out var selectedData, out var err1))
                        _ports.ForEach(p => p.Selected(selectedData));
                    else
                        Util.LogWarning($"[SimulationHandler] bad payload for {action}: {err1}");
                    break;
                case "SelectStarted":
                    _ports.ForEach(p => p.SelectStarted());
                    break;
                case "SelectEvent":
                    if (Util.TryTo<Notify.R2U.SelectEvent>(message.data, out var evtData, out var err2))
                        _ports.ForEach(p => p.SelectEvent(evtData));
                    else
                        Util.LogWarning($"[SimulationHandler] bad payload for {action}: {err2}");
                    break;
                case "Ended":
                    _ports.ForEach(p => p.Ended());
                    break;
                default:
                    Util.LogWarning($"[SimulationHandler] Unknown NTY action '{action}'");
                    break;
            }
        }

        public override void HandleRequest(Message message, Action<object> onSuccess, Action<string> onError)
        {
            var (_, action) = Util.ParseRoute(message.route);
            switch (action)
            {
                case "PreStarted":
                    {
                        if (_ports.Count == 0)
                        {
                            onError?.Invoke("No ISimulationPort registered");
                            return;
                        }

                        bool replied = false;
                        void Reply() 
                        {
                            if (replied) { Util.LogWarning("[SimulationHandler] duplicate reply ignored"); return; }
                            replied = true;
                            onSuccess?.Invoke(null);
                        }
                        void Fail(string e) 
                        {
                            if (replied) return;
                            replied = true;
                            onError?.Invoke(e);
                        }

                        _ports.ForEach(p => p.PreStarted(Reply, Fail));
                    }
                    break;
                case "PostStarted":
                    {
                        if (!Util.TryTo<Request.R2U.PostStarted>(message.data, out var req, out var err))
                        {
                            onError?.Invoke($"bad payload: {err}");
                            return;
                        }

                        if (_ports.Count == 0)
                        {
                            onError?.Invoke("No ISimulationPort registered");
                            return;
                        }

                        bool replied = false;
                        void Reply()
                        {
                            if (replied) { Util.LogWarning("[SimulationHandler] duplicate reply ignored"); return; }
                            replied = true;
                            onSuccess?.Invoke(null);
                        }
                        void Fail(string e)
                        {
                            if (replied) return;
                            replied = true;
                            onError?.Invoke(e);
                        }

                        _ports.ForEach(p => p.PostStarted(req, Reply, Fail));
                    }
                    break;
                default:
                    onError?.Invoke($"[SimulationHandler] Unknown REQ action '{action}'");
                    break;
            }
        }

        #endregion
    }
}