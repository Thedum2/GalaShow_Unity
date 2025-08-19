using System;
using Galashow.Bridge.Model;

namespace Galashow.Bridge
{
    public interface ISamplePort
    {
        void OnChangeSphereColor(Notify.R2U.ChangeSphereColor data);
        void OnCalculateAdd(Request.R2U.CalculateAdd data, Action<Acknowledge.U2R.CalculateAdd> reply, Action<string> fail);
    }
}