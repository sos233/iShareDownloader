using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Text;

namespace iShareDownloader.Utils
{
    public class ZipHelper
    {
        /// <summary>
        /// 生成ZIP压缩文件（使用.NET 4.0）
        /// 需引用 WindowsBase
        /// </summary>
        /// <param name="zipPath">传入生成的ZIP文件路径</param>
        /// <param name="absoluteUris">传入需压缩的文件绝对路径</param>
        /// <returns></returns>
        public static bool CreatePackage(string zipPath, IList<string> absoluteUris)
        {
            bool isSuc = true;
            try
            {
                using (Package package = Package.Open(zipPath, FileMode.Create))
                {
                    foreach (var uri in absoluteUris)
                    {
                        string fileName = uri.Substring(uri.LastIndexOf('/') + 1);
                        Uri partUri = PackUriHelper.CreatePartUri(new Uri(fileName, UriKind.Relative));
                        string contentType = GetContentType(fileName);
                        PackagePart packagePartDocument = package.CreatePart(partUri, contentType);

                        using (var wc = new WebClient())
                        {
                            var st = wc.DownloadData(uri);
                            packagePartDocument.GetStream().Write(st, 0, st.Length);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                isSuc = false;
            }
            return isSuc;
        }

        /// <summary>
        /// 生成ZIP压缩文件（使用.NET 4.5及以上）
        /// 需引用 System.IO.Compression 及 System.IO.Compression.FileSystem
        /// </summary>
        /// <param name="zipPath">传入生成的ZIP文件路径</param>
        /// <param name="absoluteUris">传入需压缩的文件绝对路径</param>
        /// <returns></returns>
        public static bool CreatePackageNet4(string zipPath, IList<string> absoluteUris)
        {
            bool isSuc = true;
            try
            {
                foreach (var file in absoluteUris)
                {
                    using (var wc = new WebClient())
                    {
                        var bytes = wc.DownloadData(file);
                        string fileName = file.Substring(file.LastIndexOf('/') + 1);
                        using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Update))
                        {
                            zip.CreateEntry(fileName).Open().Write(bytes, 0, bytes.Length);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                isSuc = false;
            }
            return isSuc;
        }

        //根据文件名获取ContentType
        private static string GetContentType(string fileName)
        {
            switch (fileName.Substring(fileName.IndexOf('.')).ToLower())
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                case ".zip":
                    return "application/x-zip-compressed";
                case ".pdf":
                    return "application/pdf";
                case ".htm":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".doc":
                case ".docx":
                    return "application/msword";
                case ".xls":
                case ".xlsx":
                    return "vnd.ms-excel";
                case ".mp3":
                    return "audio/mpeg3";
                default:
                    return "image/jpeg";
            }
        }
    }
}
