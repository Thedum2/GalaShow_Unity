using System;
using System.Collections.Generic;
using Galashow.Bridge.Model;
using Galashow.Core;

namespace Galashow.Bridge
{
    public sealed class RGFHandler : BaseMessageHandler
    {
        private readonly List<IRGFPort> _ports = new();

        public RGFHandler() : base("RGFManager") {}

        public void AddPort(IRGFPort port)
        {
            if (port != null && !_ports.Contains(port))
            {
                _ports.Add(port);
            }
        }

        public void RemovePort(IRGFPort port)
        {
            _ports.Remove(port);
        }

        #region =========U2R=========

        public void InitializeProgress(int currentProgress)
        {
            NTY("InitializeProgress", new Notify.U2R.RGFInitializeProgress(currentProgress));
        }

        public void PhaseChanged(int roundNumber, string miniGamePluginIdx, string fromPhase, string toPhase)
        {
            NTY("PhaseChanged", new Notify.U2R.RGFPhaseChanged(roundNumber, miniGamePluginIdx, fromPhase, toPhase));
        }

        public void PhaseStarted(string phase, float duration)
        {
            NTY("PhaseStarted", new Notify.U2R.RGFPhaseStarted(phase, duration));
        }

        public void PhaseEnded(string phase, float duration)
        {
            NTY("PhaseEnded", new Notify.U2R.RGFPhaseEnded(phase, duration));
        }

        public void RoundStarted(int roundNumber, string miniGamePluginIdx, string gameName)
        {
            NTY("RoundStarted", new Notify.U2R.RGFRoundStarted(roundNumber, miniGamePluginIdx, gameName));
        }

        public void RoundCompleted(int roundNumber, string miniGamePluginIdx, string gameName, Notify.U2R.RoundResult result)
        {
            NTY("RoundCompleted", new Notify.U2R.RGFRoundCompleted(roundNumber, miniGamePluginIdx, gameName, result));
        }

        #endregion

        #region =========R2U=========

        public override void HandleNotify(Message message)
        {
            var (_, action) = Util.ParseRoute(message.route);
            switch (action)
            {
                case "ChatInput":
                {
                    if (!Util.TryTo<Notify.R2U.RGFChatInput>(message.data, out var req, out var err))
                    {
                        GLog.Debug($"[RGFHandler] ChatInput NTY bad payload: {err}");
                        return;
                    }
                    _ports.ForEach(p => p.R2U_RGFManager_ChatInput_NTY(req));
                }
                    break;
                default:
                    GLog.Debug($"[RGFHandler] Unknown NTY action '{action}'");
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
                    if (!Util.TryTo<Request.R2U.RGFInitialize>(message.data, out var req, out var err))
                    {
                        onError?.Invoke($"bad payload: {err}");
                        return;
                    }

                    if (_ports.Count == 0)
                    {
                        onError?.Invoke("No IRGFPort registered");
                        return;
                    }

                    bool replied = false;
                    void Reply(Acknowledge.U2R.RGFInitialize data)
                    {
                        if (replied) { GLog.Debug($"[RGFHandler] duplicate reply ignored"); return; }
                        replied = true;
                        onSuccess?.Invoke(data);
                    }
                    void Fail(string e)
                    {
                        if (replied) return;
                        replied = true;
                        onError?.Invoke(e);
                    }

                    _ports.ForEach(p => p.R2U_RGFManager_Initialize_REQ(req, Reply, Fail));
                }
                    break;

                case "RegisterPlugin":
                {
                    if (!Util.TryTo<List<Request.R2U.RGFRegisterPlugin>>(message.data, out var req, out var err))
                    {
                        onError?.Invoke($"bad payload: {err}");
                        return;
                    }

                    if (_ports.Count == 0)
                    {
                        onError?.Invoke("No IRGFPort registered");
                        return;
                    }

                    bool replied = false;
                    void Reply(List<Acknowledge.U2R.RGFRegisterPlugin> data)
                    {
                        if (replied) { GLog.Debug($"[RGFHandler] duplicate reply ignored"); return; }
                        replied = true;
                        onSuccess?.Invoke(data);
                    }
                    void Fail(string e)
                    {
                        if (replied) return;
                        replied = true;
                        onError?.Invoke(e);
                    }

                    _ports.ForEach(p => p.R2U_RGFManager_RegisterPlugin_REQ(req, Reply, Fail));
                }
                    break;

                case "StartRound":
                {
                    if (!Util.TryTo<Request.R2U.RGFStartRound>(message.data, out var req, out var err))
                    {
                        onError?.Invoke($"bad payload: {err}");
                        return;
                    }

                    if (_ports.Count == 0)
                    {
                        onError?.Invoke("No IRGFPort registered");
                        return;
                    }

                    bool replied = false;
                    void Reply(Acknowledge.U2R.RGFStartRound data)
                    {
                        if (replied) { GLog.Debug($"[RGFHandler] duplicate reply ignored"); return; }
                        replied = true;
                        onSuccess?.Invoke(data);
                    }
                    void Fail(string e)
                    {
                        if (replied) return;
                        replied = true;
                        onError?.Invoke(e);
                    }

                    _ports.ForEach(p => p.R2U_RGFManager_StartRound_REQ(req, Reply, Fail));
                }
                    break;

                case "AbortRound":
                {
                    if (!Util.TryTo<Request.R2U.RGFAbortRound>(message.data, out var req, out var err))
                    {
                        onError?.Invoke($"bad payload: {err}");
                        return;
                    }

                    if (_ports.Count == 0)
                    {
                        onError?.Invoke("No IRGFPort registered");
                        return;
                    }

                    bool replied = false;
                    void Reply(Acknowledge.U2R.RGFAbortRound data)
                    {
                        if (replied) { GLog.Debug($"[RGFHandler] duplicate reply ignored"); return; }
                        replied = true;
                        onSuccess?.Invoke(data);
                    }
                    void Fail(string e)
                    {
                        if (replied) return;
                        replied = true;
                        onError?.Invoke(e);
                    }

                    _ports.ForEach(p => p.R2U_RGFManager_AbortRound_REQ(req, Reply, Fail));
                }
                    break;

                default:
                    onError?.Invoke($"[RGFHandler] Unknown REQ action '{action}'");
                    break;
            }
        }

        #endregion
    }
}
