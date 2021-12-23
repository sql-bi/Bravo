using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.IO;

namespace Sqlbi.Bravo.Infrastructure.Configuration.Options
{
    internal class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfigurationRoot _configuration;
        private readonly IOptionsMonitor<T> _options;
        private readonly string _section;
        private readonly string _file;

        public WritableOptions(IWebHostEnvironment environment, IConfigurationRoot configuration, IOptionsMonitor<T> options, string section, string file)
        {
            _environment = environment;
            _configuration = configuration;
            _options = options;
            _section = section;
            _file = file;
        }

        public T Value => _options.CurrentValue;

        public T Get(string name) => _options.Get(name);

        public void Update(Action<T> update)
        {
            var fileProvider = _environment.ContentRootFileProvider;
            var fileInfo = fileProvider.GetFileInfo(_file);
            var filePath = fileInfo.PhysicalPath;

            // Here Newtonsoft.Json is used because System.Text.Json DOM is read-only on .NET5

            if (fileInfo.Exists)
            {
                var fileContent = File.ReadAllText(filePath);
                var fileJObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(fileContent) ?? throw new BravoException($"Unexpected error");

                var sectionObject = fileJObject.TryGetValue(_section, out var section)
                    ? Newtonsoft.Json.JsonConvert.DeserializeObject<T>(section.ToString())
                    : Value;

                if (sectionObject is null)
                    sectionObject = new T();

                update(sectionObject);
                
                var sectionJson = Newtonsoft.Json.JsonConvert.SerializeObject(sectionObject);
                fileJObject[_section] = Newtonsoft.Json.Linq.JObject.Parse(sectionJson);

                fileContent = Newtonsoft.Json.JsonConvert.SerializeObject(fileJObject, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, fileContent);
            }
            else
            {
                var sectionObject = Value ?? new T();

                update(sectionObject);

                var fileJObject = new Newtonsoft.Json.Linq.JObject();
                var sectionJson = Newtonsoft.Json.JsonConvert.SerializeObject(sectionObject);
                fileJObject[_section] = Newtonsoft.Json.Linq.JObject.Parse(sectionJson);

                var fileContent = Newtonsoft.Json.JsonConvert.SerializeObject(fileJObject, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, fileContent);
            }

            _configuration.Reload();
        }
    }
}
