using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils;
using System.Text;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CloudBuildUnitTests
{
    public class KeyValue
    {
        public string key { get; set; }
        public string value { get; set; }
    }
    [TestClass]
    public class End2EndTests
    {
        // todo: update based on deployment url
        private const string serviceUrl = "http://localhost:8080/";

        /// <summary>
        /// we trigger build in the cloud for our source code, wait for it to finish and pull
        /// the binary. We then run it and validate expected output
        /// </summary>
        [TestMethod]
        public void full_cycle_happy_flow()
        {
            // upload source code
            string sourceCode =
                @"public class Program
                {
                    public static void Main()
                    {
                        string result = CheckMe();
                        System.Console.Write(result);
                    }

                    public static string CheckMe()
                    {
                        int i = 10;
                        string result = $""hello World!. i={i}"";
                        return result;
                    }
                }";

            byte[] toBytes = Encoding.ASCII.GetBytes(sourceCode);
            string sourceName = "full_cycle_happy_flow";
            BlobClient.WriteBlob("input", $"{sourceName}.cs", toBytes);

            // trigger build
            string buildUrl = $"{serviceUrl}api/Builds/{sourceName}";
            using (var client = new System.Net.WebClient())
            {
                var byteArray = new byte[0];
                client.UploadData(buildUrl, "PUT", byteArray);
            }

            string result = string.Empty;
            while (result !=null && !(result.Contains("Success") || result.Contains("Failed")))
            {
                // get status
                result = GetBuildResult(sourceName);
            }

            Assert.IsTrue(result.Contains("Success"));

            // now get binary and run it
            string exeUrl = $"https://amitstorage11.blob.core.windows.net/output/{sourceName}.exe_";

            WebClient webClient = new WebClient();
            var exe = webClient.DownloadData(exeUrl);

            // Load the resulting assembly into the domain. 
            Assembly assembly = Assembly.Load(exe);

            // get the type Program from the assembly
            Type programType = assembly.GetType("Program");

            // Get the static Main() method info from the type
            MethodInfo method = programType.GetMethod("CheckMe");

            // invoke Program.Main() static method
            string runResult = (string) method.Invoke(null, null);

            Assert.AreEqual("hello World!. i=10", runResult);

        }

        private string GetBuildResult(string sourceName)
        {
            // send get request to get build list
            string buildUrl = $"{serviceUrl}api/Builds";

            WebClient client = new WebClient();
            string reply = client.DownloadString(buildUrl);

            IEnumerable<KeyValue> d = JsonConvert.DeserializeObject<List<KeyValue>>(reply);

            string result = d.FirstOrDefault(x => x.key == sourceName).value;

            return result;
        }
    }
}
