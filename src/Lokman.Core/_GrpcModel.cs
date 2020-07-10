namespace Lokman
{
    public class LockRequest
    {
        public string Key { get; set; }
        public long Expiration { get; set; }
        public long Index { get; set; }
    }

    public class LockResponse
    {
        public string Key { get; set; }
        public long Ticks { get; set; }
        public long Index { get; set; }
    }
}