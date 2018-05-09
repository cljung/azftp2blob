using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using AzureFtpServer.Ftp;

namespace AzureFtpServer.Security
{
    /// <summary>
    /// Provides basic protection mechanism for password brute force attempts.
    /// Counts number of invalid login attempts in given time period; if
    /// number of invalid attempts exceeds specified threshold, this login
    /// will be blocked for some time.
    /// </summary>
    public sealed class InvalidAttemptCounter
    {
        private readonly InvalidLogonCheckOptions opts;

        /// <summary>
        /// Stores number of invalid login attempts foreach login
        /// </summary>
        private readonly MemoryCache invalidAttemptsCount = new MemoryCache("invalid-login-attempts");

        private readonly MemoryCache blockedUsers = new MemoryCache("blocked-users");

        public InvalidAttemptCounter(InvalidLogonCheckOptions opts)
        {
            if (opts == null)
            {
                throw new ArgumentNullException(nameof(opts));
            }

            this.opts = opts;
        }

        private class InvalidLogonRecord
        {
            /// <summary>
            /// Invalid logon attempts dates, sorted by date ascending
            /// </summary>
            public List<DateTimeOffset> InvalidAttemptsDates { get; } = new List<DateTimeOffset>();

            public DateTimeOffset ExpirationDate { get; set; }

            public InvalidLogonRecord(DateTimeOffset loginDate, TimeSpan checkPeriod)
            {
                InvalidAttemptsDates.Add(loginDate);
                ExpirationDate = loginDate.Add(checkPeriod);
            }

            public void ClearAttemptsBefore(DateTimeOffset threshold)
            {
                while (InvalidAttemptsDates.Count > 0)
                {
                    DateTimeOffset toCheck = InvalidAttemptsDates[0];
                    if (toCheck <= threshold)
                    {
                        InvalidAttemptsDates.RemoveAt(0);
                    }
                    else
                    {
                        //all other dates will be larger, no point to check further
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Registers invalid logon attempt
        /// </summary>
        /// <param name="user">login for which invalid password was given</param>
        public void OnInvalidLogin(string user)
        {
            if (!opts.Enabled)
            {
                return;
            }

            DateTimeOffset invalidLoginDate = DateTimeOffset.Now;
            var invalidLoginRecord = new InvalidLogonRecord(invalidLoginDate, opts.CheckPeriod);
            lock (invalidAttemptsCount)
            {
                object val = invalidAttemptsCount.AddOrGetExisting(user, invalidLoginRecord, invalidLoginRecord.ExpirationDate);
                if (val is InvalidLogonRecord prev)
                {
                    //there is already record of invalid logons for this user
                    //add fresh invalid logon date
                    prev.InvalidAttemptsDates.Add(invalidLoginDate);
                    //clear invalid login attempts which are outside of check period
                    DateTimeOffset threshold = invalidLoginDate.Subtract(opts.CheckPeriod);
                    prev.ClearAttemptsBefore(threshold);

                    //update data in cache
                    invalidAttemptsCount.Set(user, prev, invalidLoginRecord.ExpirationDate);

                    if (prev.InvalidAttemptsDates.Count > opts.MaxInvalidAttempts)
                    {
                        DateTimeOffset blockTill = invalidLoginDate.Add(opts.BlockPeriod);
                        FtpServer.LogWrite($"max number of invalid login attempts exceeded for login {user}, " +
                                           $"will block it till {blockTill}");
                        blockedUsers.Set(user, user, blockTill);
                    }
                }
            }
        }

        /// <summary>
        /// Registeres successful logon attempt.
        /// </summary>
        /// <param name="user">login which logged in</param>
        public void OnSuccesfullLogin(string user)
        {
            if (!opts.Enabled)
            {
                return;
            }

            lock (invalidAttemptsCount)
            {
                invalidAttemptsCount.Remove(user);
            }
        }

        /// <summary>
        /// Should we allow user command
        /// </summary>
        /// <param name="user">user login</param>
        /// <returns></returns>
        public bool IsUserAllowed(string user)
        {
            if (!opts.Enabled)
            {
                return true;
            }

            bool blocked = false;
            lock (invalidAttemptsCount)
            {
                object o = blockedUsers.Get(user);
                blocked = o != null;
            }

            if (blocked)
            {
                return false;
            }

            return true;
        }
    }
}