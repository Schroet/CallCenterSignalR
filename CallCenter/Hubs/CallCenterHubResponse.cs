namespace CallCenter.Hubs
{
    public class CallCenterHubResponse
    {
        public string Message { get; set; }
        public int FreeOperators { get; set; }
        public int FreeManagers { get; set; }
        public int FreeSeniorManagers { get; set; }
    }
}
