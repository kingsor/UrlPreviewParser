using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UrlPreviewParser;
using UrlPreviewParser.Models;
using Formatting = Newtonsoft.Json.Formatting;

namespace ConsoleSample
{
    class PreviewClient
    {
        public async Task Start()
        {
            while (true)
            {
                Console.Write("Preview URL: ");
                var url = Console.ReadLine();

                await Preview(url);
            }
        }

        private void InvalidUrl(string message)
        {
            Console.WriteLine("An error occurred - {0}", message);
        }

        private async Task Preview(string url)
        {
            UrlPreviewResult preview;
            try
            {
                
                var previewParser = new UrlPreviewClient();
                preview = await previewParser.GetPreviewAsync(url);

                var jsonResolver = new PropertyIgnoreContractResolver();
                jsonResolver.IgnoreProperty(typeof(UrlPreviewResult), "Body");

                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.ContractResolver = jsonResolver;
                serializerSettings.Formatting = Formatting.Indented;

                var serialized = JsonConvert.SerializeObject(preview, serializerSettings);
                Console.WriteLine(serialized);
            }
            catch (Exception ex)
            {
                InvalidUrl(ex.Message);
            }

            

            //try
            //{
            //    var graph = await OpenGraph.ParseUrlAsync(url);

            //    var jsonResolver = new PropertyRenameAndIgnoreContractResolver();
            //    jsonResolver.IgnoreProperty(typeof(OpenGraph), "OriginalHtml");

            //    var serializerSettings = new JsonSerializerSettings();
            //    serializerSettings.ContractResolver = jsonResolver;
            //    serializerSettings.Formatting = Formatting.Indented;

            //    var jsonGraph = JsonConvert.SerializeObject(graph, serializerSettings);
            //    Console.WriteLine(jsonGraph);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error: {ex.Message}");
            //}
        }
    }
}
