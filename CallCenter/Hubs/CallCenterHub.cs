﻿using CallCenter.Models;
using CallCenter.Services;
using Microsoft.AspNetCore.SignalR;
using System;
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
                Console.WriteLine("Method ended");
            }
        }

        public void Restart(SimulationOptions options)
        {
            _callCenter.Restart();
        }

        public void Stop()
        {
            if (_callCenter.IsRunning)
            {
                _callCenter.Stop();
            }
        }

    }
}
