using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace ClassIsland.Helpers
{
    public static class StorageSizeHelper
    {
        /// <summary>
        /// 将存储空间大小转换为易读形式
        /// </summary>
        /// <param name="size">原始存储空间大小(Byte)</param>
        /// <returns>易读形式文件大小(KiB、MiB etc.)（保留两位小数）</returns>
        public static string FormatSize(ulong size)
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
        /// <summary>
        /// 获取指定文件夹的存储空间占用
        /// </summary>
        /// <param name="path">文件夹位置</param>
        /// <returns>返回指定文件夹的存储空间占用，单位为Bytes</returns>
        /// <remarks>本方法采用递归来计算指定文件夹内所有文件的大小并加和，请考虑此方法的用处(可能造成性能负担)<para/>本方法若遇到异常则返回0</remarks>
        public static ulong GetFolderStorageSize(string path)
        {
            ulong size = 0;
            if (!Path.Exists(path)) return 0;
            try
            {
                DirectoryInfo dirInfo = new(path);
                foreach (FileInfo fileInfo in dirInfo.GetFiles())
                {
                    size += (ulong)fileInfo.Length;
                }
                foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
                {
                    size += GetFolderStorageSize(subDir.FullName);
                }
            }
            catch
            {
                return 0;
            }
            return size;
        }
    }
}
