using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Datasheets2
{
    /// <summary>
    /// Allows API keys to be stored outside the source code, in an XML file.
    /// The XML file should look like this:
    ///   <?xml version="1.0" encoding="utf-8" ?>
    ///   <ApiKeys>
    ///     <ApiKey Name="ApiName" SecretKey="abcdef"/>  
    ///    </ApiKeys>
    /// </summary>
    [XmlRoot("ApiKeys")]
    public class ApiKeyManager : List<ApiKey>
    {
        private Dictionary<string, ApiKey> apiKeys;
        private string filename;

        public static ApiKeyManager LoadFromFile(string filename)
        {
            //var serializer = XmlSerializer();
            var apiman = new ApiKeyManager();
            apiman.filename = filename;
            return apiman;
        }

        public static ApiKeyManager LoadFromResource(string resourceName)
        {
            ApiKeyManager apiman;

            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName().Name;
            var stream = assembly.GetManifestResourceStream($"{assemblyName}.{resourceName}");
            if (stream != null)
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(ApiKeyManager));
                    apiman = (ApiKeyManager)serializer.Deserialize(stream);
                }
                finally
                {
                    stream.Close();
                    stream.Dispose();
                }
            }
            else
            {
                // No resource found, create an empty manager
                Debug.WriteLine($"WARNING: {resourceName} not found");

                apiman = new ApiKeyManager();
            }

            apiman.filename = resourceName;
            return apiman;
        }

        public ApiKey GetKey(string name)
        {
            if (apiKeys == null)
            {
                apiKeys = new Dictionary<string, ApiKey>();
                foreach (var k in this)
                {
                    apiKeys.Add(k.Name, k);
                }
            }

            ApiKey key;
            if (!apiKeys.TryGetValue(name, out key))
            {
                // If not defined in ApiKeys.xml, try App.config...
                key = Settings.GetApiKey(name);
                if (key == null)
                    throw new KeyNotFoundException($"No API keys available for {name}. Ensure you've added it to {filename}");
            }

            return key;
        }
    }

    public class ApiKey
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string ClientID { get; set; }

        [XmlAttribute]
        public string SecretKey { get; set; }
    }
}
