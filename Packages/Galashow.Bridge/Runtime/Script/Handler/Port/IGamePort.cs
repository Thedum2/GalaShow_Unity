using System;
using Galashow.Bridge.Model;

namespace Galashow.Bridge
{
    public interface IGamePort
    {
        void Initialize(Request.R2U.Initialize data, Action<Acknowledge.U2R.Initialize> onSuccess, Action<string> onError);
        void GameEnded();
    }
}