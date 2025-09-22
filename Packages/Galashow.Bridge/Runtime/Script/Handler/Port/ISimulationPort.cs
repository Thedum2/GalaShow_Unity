using System;
using Galashow.Bridge.Model;

namespace Galashow.Bridge
{
    public interface ISimulationPort
    {
        void Selected(Notify.R2U.Selected data);
        void PreStarted(Action onSuccess, Action<string> onError);
        void SelectStarted();
        void SelectEvent(Notify.R2U.SelectEvent data);
        void PostStarted(Request.R2U.PostStarted data, Action onSuccess, Action<string> onError);
        void Ended();
    }
}