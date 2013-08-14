using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if !UNITY_WEBPLAYER

using System.IO;

#endif

namespace Gameverses {

    public class FileSystemUtil {

        public static void CopyFile(string dataFilePath, string persistenceFilePath) {
#if !UNITY_WEBPLAYER
            if (File.Exists(dataFilePath) && !File.Exists(persistenceFilePath)) {

                // Copy file if it does not exist
                File.Copy(dataFilePath, persistenceFilePath, true);
            }
#endif
        }

        public static void MoveFile(string dataFilePath, string persistenceFilePath) {
#if !UNITY_WEBPLAYER
            if (File.Exists(dataFilePath) && !File.Exists(persistenceFilePath)) {
                File.Move(dataFilePath, persistenceFilePath);
            }
#endif
        }

        public static byte[] ReadAllBytes(string fileName) {
#if !UNITY_WEBPLAYER
            return File.ReadAllBytes(fileName);
#endif
        }

        public static void WriteAllBytes(string fileName, byte[] buffer) {
#if !UNITY_WEBPLAYER
            File.WriteAllBytes(fileName, buffer);
#endif
        }

        public static byte[] ReadStream(string fileName) {
            byte[] buffer = null;
#if !UNITY_WEBPLAYER
            if (File.Exists(fileName)) {
                FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);
                long length = new FileInfo(fileName).Length;
                buffer = br.ReadBytes((int)length);
                br.Close();
                fs.Close();
            }
#endif
            return buffer;
        }

        public static void WriteStream(string fileName, byte[] data) {
#if !UNITY_WEBPLAYER
            StreamWriter sw = new StreamWriter(fileName, false, Encoding.ASCII);
            sw.Write(data);
            sw.Flush();
            sw.Close();
#endif
        }

        public static string ReadString(string fileName) {
            string contents = "";
#if !UNITY_WEBPLAYER
            if (File.Exists(fileName)) {
                StreamReader sr = new StreamReader(fileName, true);
                contents = sr.ReadToEnd();
                sr.Close();
            }
#endif
            return contents;
        }

        public static void WriteString(string fileName, string data) {
#if !UNITY_WEBPLAYER
            StreamWriter sw = new StreamWriter(fileName, false, Encoding.ASCII);
            sw.Write(data);
            sw.Flush();
            sw.Close();
#endif
        }
    }
}