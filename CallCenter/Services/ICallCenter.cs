using CallCenter.Models;

namespace CallCenter.Services
{
    public interface ICallCenter
    {
        void Start(SimulationOptions options);
        void Pause();
        void Stop();
        bool IsRunning { get; }
        void SendCall();
    }
}
