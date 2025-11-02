using Galashow.Core;
using UnityEngine;

namespace Galashow.Bridge
{
    public class MainHandler
    {
        public RGFHandler RGFHandler { get; private set; }

        public void Initialize()
        {
            RGFHandler = new RGFHandler();
        }
    }
}