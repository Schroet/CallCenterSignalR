using CallCenter.Models;

namespace CallCenter.Services
{
    public interface ICallCenter
    {
        void Start(SimulationOptions options);
        void Restart();
        void Stop();
        bool IsRunning { get; }
    }
}
