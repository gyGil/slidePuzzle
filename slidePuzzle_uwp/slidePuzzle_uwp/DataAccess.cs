using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace slidePuzzle_uwp
{
    public static class DataAccess
    {
        public static List<Player> list = new List<Player>();
        /// <summary>
        /// write the content of app to local file
        /// referece to https://msdn.microsoft.com/en-us/library/windows/apps/mt299098
        /// </summary>
        public static async void WriteLocalFile(List<Player> list)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            StorageFile f = await localFolder.CreateFileAsync("myPuzzlePlayers.txt",
                CreationCollisionOption.ReplaceExisting);

            // serialize the object to a memory stream and write to stream
            MemoryStream memStream = new MemoryStream();
            DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof(List<Player>));
            jsonSer.WriteObject(memStream,list); // referece to https://blog.udemy.com/json-serializer-c-sharp/       
            await FileIO.WriteBytesAsync(f, memStream.ToArray());   // write to bytes 
        }

        /// <summary>
        /// read the content of app from local file
        /// referece to https://msdn.microsoft.com/en-us/library/windows/apps/mt299098
        /// </summary>
        /// <returns></returns>
        public static async void ReadLocalFile()
        {
            DataAccess.list = new List<Player>();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            try
            {
                StorageFile f = await localFolder.GetFileAsync("myPuzzlePlayers.txt");  // get app local file
                MemoryStream memStream = new MemoryStream();
                DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof(List<Player>));
                string content = await FileIO.ReadTextAsync(f); // read file to string
                DataAccess.list = jsonSer.ReadObject(memStream) as List<Player>;
            }
            catch (Exception)
            {
                DataAccess.list = null;
            }       
        }
    }
}
