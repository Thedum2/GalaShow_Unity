using System;
using System.Collections.Generic;
using Galashow.Bridge.Model;

namespace Galashow.Bridge
{
    public interface IRGFPort
    {
        void R2U_RGFManager_Initialize_REQ(
            Request.R2U.RGFInitialize data,
            Action<Acknowledge.U2R.RGFInitialize> onSuccess,
            Action<string> onError);

        void R2U_RGFManager_RegisterPlugin_REQ(
            List<Request.R2U.RGFRegisterPlugin> data,
            Action<List<Acknowledge.U2R.RGFRegisterPlugin>> onSuccess,
            Action<string> onError);

        void R2U_RGFManager_StartRound_REQ(
            Request.R2U.RGFStartRound data,
            Action<Acknowledge.U2R.RGFStartRound> onSuccess,
            Action<string> onError);

        void R2U_RGFManager_AbortRound_REQ(
            Request.R2U.RGFAbortRound data,
            Action<Acknowledge.U2R.RGFAbortRound> onSuccess,
            Action<string> onError);

        void R2U_RGFManager_ChatInput_NTY(Notify.R2U.RGFChatInput data);
    }
}
