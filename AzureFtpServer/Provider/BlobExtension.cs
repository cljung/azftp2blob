using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureFtpServer.Provider
{
    internal static class BlobExtension
    {
        private const string CreationTimeKey = "CreationTime";
        private const string DateFormatIso8601 = "O";
        public static void SetCreationTime(this CloudBlob blob)
        {
            blob.Metadata[CreationTimeKey] = DateTimeOffset.UtcNow.ToString(DateFormatIso8601);
        }

        public static DateTimeOffset? GetCreationTime(this CloudBlob blob)
        {
            if (blob.Metadata.TryGetValue(CreationTimeKey, out string dateInMeta))
            {
                return DateTimeOffset.ParseExact(dateInMeta, DateFormatIso8601, CultureInfo.InvariantCulture);
            }

            return null;
        }
    }
}
