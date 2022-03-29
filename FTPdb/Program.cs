using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

//using System.Threading.Tasks;

namespace FTPdb
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string ftp_ip = Properties.Settings.Default.ftpIp;
            string login_id = Properties.Settings.Default.loginId;
            string login_pw = Properties.Settings.Default.loginPw;
            string downloadFile = string.Empty;
            string nameRule = Properties.Settings.Default.nameRule;
            string local_Path = Properties.Settings.Default.localPath;
            int bufferSize = 2048;

            Regex regex = new Regex(nameRule);
            Match matchRex;

            #region FTP列出Server檔案清單

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftp_ip);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.UseBinary = true;
            request.Credentials = new NetworkCredential(login_id, login_pw);
            WebResponse response = request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, Encoding.Default);

            #endregion FTP列出Server檔案清單

            #region 用正規表示法比對server檔案清單 若數字比較大就抓最大的(最新)

            string line = reader.ReadLine();
            while (line != null)
            {
                matchRex = regex.Match(line);

                if (matchRex.Success)
                {
                    if (downloadFile == string.Empty)
                        downloadFile = line;
                    else if (string.Compare(line, downloadFile) > 0)
                        downloadFile = line;
                }

                line = reader.ReadLine();
            }

            //Console.WriteLine(downloadFile);

            #endregion 用正規表示法比對server檔案清單 若數字比較大就抓最大的(最新)

            reader.Close();
            responseStream.Close();
            response.Close();
            request = null;

            #region 開始FTP下載

            if (downloadFile != string.Empty) //先確定server有檔案可以下載
            {
                if (!File.Exists(local_Path + downloadFile)) //如果本機沒有這個檔案再下載
                {
                    string[] localFiles = Directory.GetFiles(local_Path);
                    foreach (string localFile in localFiles)
                    {
                        matchRex = regex.Match(localFile);
                        if (matchRex.Success && !Path.GetFileName(localFile).Equals(downloadFile))
                            File.Delete(Path.GetFullPath(localFile));  //刪除掉之前的檔案
                        //Console.WriteLine((Path.GetFullPath(localFile)));
                    }

                    //底下為FTP下載
                    request = (FtpWebRequest)WebRequest.Create(ftp_ip + downloadFile);
                    request.Method = WebRequestMethods.Ftp.DownloadFile;
                    request.UseBinary = true;
                    request.Credentials = new NetworkCredential(login_id, login_pw);
                    response = request.GetResponse();
                    responseStream = response.GetResponseStream();
                    reader = new StreamReader(responseStream, Encoding.Default);

                    FileStream localFileStream = new FileStream(local_Path + downloadFile, FileMode.Create);
                    byte[] byteBuffer = new byte[bufferSize];
                    int bytesRead = responseStream.Read(byteBuffer, 0, bufferSize);
                    while (bytesRead > 0)
                    {
                        localFileStream.Write(byteBuffer, 0, bytesRead);
                        bytesRead = responseStream.Read(byteBuffer, 0, bufferSize);
                    }

                    localFileStream.Close();
                    reader.Close();
                    responseStream.Close();
                    response.Close();
                    request = null;
                }
            }

            #endregion 開始FTP下載

            //Console.WriteLine("\n\n\n--work to end--\n--press q to exit--");
            //while ("q" != Console.ReadLine()) ;
        }
    }
}