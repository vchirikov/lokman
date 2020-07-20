using System;

namespace Lokman
{
    public class LockInfo
    {
        public readonly string Key;
        public readonly bool IsLocked;
        public readonly long Token;
        public readonly DateTime ExpirationUtc;

        public LockInfo(string key, bool isLocked, long token, DateTime expirationUtc)
        {
            Key = key;
            IsLocked = isLocked;
            Token = token;
            ExpirationUtc = expirationUtc;
        }
    }
}