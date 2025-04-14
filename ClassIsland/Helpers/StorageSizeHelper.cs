using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassIsland.Helpers
{
    public static class StorageSizeHelper
    {
        public static string FormatSize(long size)
        {
            string[] suffixes = { " B", " KiB", " MiB", " GiB", " TiB", " PiB" };
            double last = 1;
            for (int i = 0; i < suffixes.Length; i++)
            {
                var current = Math.Pow(1024, i + 1);
                var temp = size / current;
                if (temp < 1) return (size / last).ToString("n2") + suffixes[i];
                last = current;
            }
            return size.ToString();
        }
    }
}
