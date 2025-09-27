using System;
using Galashow.Bridge.Model;

namespace Galashow.Bridge
{
    public interface IGamePort
    {
        void R2U_GameManager_Initialize_REQ(Request.R2U.Initialize data, Action<Acknowledge.U2R.Initialize> onSuccess, Action<string> onError);
        void R2U_GameManager_Ended_NTY();
    }
}
