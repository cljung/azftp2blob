using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFtpServer.Security
{
    /// <summary>
    /// Configuration options for limiting password brute force 
    /// attempts. Values are read from configuration.
    /// </summary>
    public sealed class InvalidLogonCheckOptions
    {
        /// <summary>
        /// Period of time, in which we count invalid logon attempts.
        /// </summary>
        public TimeSpan CheckPeriod { get; }
        /// <summary>
        /// Period of time, which specific login is blocked for, when
        /// <see cref="MaxInvalidAttempts"/> limit is exceeded.
        /// </summary>
        public TimeSpan BlockPeriod { get; }
        /// <summary>
        /// Is invalid logon check enabled.
        /// </summary>
        public bool Enabled { get; }
        /// <summary>
        /// Max number of invalid login attempts in <see cref="CheckPeriod"/>.
        /// Exceeding this value will cause subsequent login attempts blocked
        /// in next <see cref="BlockPeriod"/> time.
        /// </summary>
        public int MaxInvalidAttempts { get; }

        private const string CommonPrefix = "ftp.security.invalid-logon";

        public InvalidLogonCheckOptions()
        {
            MaxInvalidAttempts = ReadIntFromConfig($"{CommonPrefix}.max-attempts", false) ?? 5;
            Enabled = MaxInvalidAttempts > -1;
            if (!Enabled)
            {
                return;
            }

            int checkSeconds = ReadIntFromConfig($"{CommonPrefix}.check-period-seconds", true) ?? 60;
            CheckPeriod = TimeSpan.FromSeconds(checkSeconds);

            int blockSeconds = ReadIntFromConfig($"{CommonPrefix}.block-period-second", true) ?? 180;
            BlockPeriod = TimeSpan.FromSeconds(blockSeconds);
        }

        private int? ReadIntFromConfig(string key, bool mustBePositive)
        {
            string val = ConfigurationManager.AppSettings[key];
            if (int.TryParse(val, out int result))
            {
                return result;
            }
            else if (!string.IsNullOrWhiteSpace(val))
            {
                throw new ConfigurationErrorsException($"not a valid int value in {key} configuration key: {val}");
            }

            if (mustBePositive && result < 0)
            {
                throw new ConfigurationErrorsException($"negative value is not allowed for {key} configuration key");
            }

            return null;
        }
    }
}
