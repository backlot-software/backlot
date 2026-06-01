using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Backlot.Core;
using Backlot.Core.Json.Serialization.Newtonsoft;

namespace Backlot.Http.Media.Formatters.Csv
{
    public class CsvFormatter : IMediaFormatter
    {
        public string MediaType => "application/csv";

        public async Task<ResponseData> GetResponse<T>(RequestData req, T returnObj, Stopwatch stopwatch, HttpStatusCode statusCode=HttpStatusCode.OK)
        {
            var serializedJToken = returnObj is JToken obj ? obj :  
                // We do NOT serialize for Interaction here because we want to have the CSV as clean as possible.
                JToken.FromObject(returnObj is IResultCollection rc ? rc.Results : returnObj, Strategy.SerializeSafe);

            var records = new List<IDictionary<string, object>>();

            if (serializedJToken is JArray arr)
            {
                records.AddRange(arr.Select(itm => FlattenJson.Execute(itm)));
            }
            else
            {
                records.Add(FlattenJson.Execute(serializedJToken));
            }

            await using var writer = new StringWriter();
            await using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
            
            //create header
            var headers = records.OrderByDescending(r => r.Count()).SelectMany(itm => itm.Keys).Distinct().ToList();
            headers.ForEach(itm => csv.WriteField(itm));
            await csv.NextRecordAsync();

            foreach (var row in records)
            {
                foreach (var header in headers)
                {
                    if (row.TryGetValue(header, out var value))
                        csv.WriteField(value);
                    else
                        csv.WriteField(string.Empty);
                }

                await csv.NextRecordAsync();
            }
            
            var response = new ResponseData
            {
                Content = writer.ToString(),
                StatusCode = statusCode
            };

            response.Headers.Add("Content-Type", MediaType);
            response.Headers.Add("Content-Disposition", "attachment;filename=Export.csv");
            
            return response;
        }
    }
}