using CallCenter.Models;
using CallCenter.Services;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace CallCenter.Hubs
{
    public class CallCenterHub : Hub
    {
        private readonly ICallCenter _callCenter;
        public CallCenterHub(ICallCenter callCenter)
        {
            _callCenter = callCenter;
        }

        public void Start(SimulationOptions options)
        {
            if (!_callCenter.IsRunning)
            {
                _callCenter.Start(options);
            }   
        }

        public void Restart(SimulationOptions options)
        {
            Stop();
            Start(options);
           // Debug.WriteLine("restart executed");
        }

        public void Stop()
        {
            if (_callCenter.IsRunning)
            {
                _callCenter.Stop();
            }
        }

        public void SendCall()
        {
            if (_callCenter.IsRunning)
            {
                _callCenter.SendCall();
            }
        }

    }
}
