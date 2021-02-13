using Android.App;
using Android.Content;
using Android.Gms.Drive;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoCgDrive.Services
{
    public class BackupMetadata
    {
        public BackupMetadata()
        {
        }

        public BackupMetadata(Metadata metadataGms)
        {
            if (metadataGms != null)
            {
                MetadataGms = metadataGms;
                LastBackup = ToDateTime(metadataGms.ModifiedDate);
                SizeInBytes = metadataGms.FileSize;
            }
        }

        public Metadata MetadataGms { get; }

        public DateTime LastBackup { get; }

        private long SizeInBytes { get; }

        public bool HasValue { get { return LastBackup != default && SizeInBytes > 0; } }

        private DateTime ToDateTime(Java.Util.Date javaDate)
        {
            var unixTimeMillis = javaDate.Time;
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(unixTimeMillis);
        }

        public string GetSizeDescription()
        {
            if (SizeInBytes == 0)
            {
                return string.Empty;
            }

            var decimalFormat = "0.#";

            if (SizeInBytes > 1 * 1024 * 1024)
            {
                var valueInMegabytes = Convert.ToDecimal(SizeInBytes) / 1024.0M / 1024.0M;
                return $"{valueInMegabytes.ToString(decimalFormat)}MB";
            }

            if (SizeInBytes > 1 * 1024)
            {
                var valueInKilobytes = Convert.ToDecimal(SizeInBytes) / 1024.0M;
                return $"{valueInKilobytes.ToString(decimalFormat)}kB";
            }

            var value = SizeInBytes;
            return $"{value}B";
        }
    }
}