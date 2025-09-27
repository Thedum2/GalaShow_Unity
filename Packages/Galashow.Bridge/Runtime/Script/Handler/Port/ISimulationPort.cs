using System;
using Galashow.Bridge.Model;

namespace Galashow.Bridge
{
    public interface ISimulationPort
    {
        void R2U_SimulationManager_Selected_NTY(Notify.R2U.Selected data);
        void R2U_SimulationManager_PreStarted_REQ(Action onSuccess, Action<string> onError);
        void R2U_SimulationManager_SelectStarted_NTY();
        void R2U_SimulationManager_SelectEvent_NTY(Notify.R2U.SelectEvent data);
        void R2U_SimulationManager_PostStarted_REQ(Request.R2U.PostStarted data, Action onSuccess, Action<string> onError);
        void R2U_SimulationManager_Ended_NTY();
    }
}