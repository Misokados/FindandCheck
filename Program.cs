using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        string referenceFilePath = @"D:\apps\Visual Studio\repos\FindAndCheck\another config\default.json";
        string sampleFilePath = @"D:\apps\Visual Studio\repos\FindAndCheck\default.json";

        try
        {
            var referenceJson = File.ReadAllText(referenceFilePath);
            var sampleJson = File.ReadAllText(sampleFilePath);

            var referenceObject = JsonConvert.DeserializeObject<JObject>(referenceJson);
            var sampleObject = JsonConvert.DeserializeObject<JObject>(sampleJson);

            var differences = CompareObjects(referenceObject, sampleObject);

            if (differences.Any())
            {
                Console.WriteLine("Find the difference in the config files:");
                Console.WriteLine(new string('-', 50));
                Console.WriteLine($"{"Parameter",-30} | {"sample",-15} | {"user's file",-15}");
                Console.WriteLine(new string('-', 50));
                foreach (var diff in differences)
                {
                    Console.WriteLine($"{diff.Key,-30} | {diff.Value.SampleValue,-15} | {diff.Value.ReferenceValue,-15}");
                }
            }
            else
            {
                Console.WriteLine("There's no difference");
            }
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Cant find file: {ex.FileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Some error: {ex.Message}");
        }
    }

    static Dictionary<string, ValueDifference> CompareObjects(JObject reference, JObject sample, string parentKey = "")
    {
        var differences = new Dictionary<string, ValueDifference>();

        foreach (var prop in reference.Properties())
        {
            string fullKey = string.IsNullOrEmpty(parentKey) ? prop.Name : $"{parentKey}.{prop.Name}";

            if (!sample.TryGetValue(prop.Name, out JToken sampleValue))
            {
                differences.Add(fullKey, new ValueDifference(prop.Value, null));
                continue;
            }

            if (prop.Value.Type == JTokenType.Object)
            {
                var nestedDifferences = CompareObjects((JObject)prop.Value, (JObject)sampleValue, fullKey);
                foreach (var nestedDiff in nestedDifferences)
                {
                    differences.Add(nestedDiff.Key, nestedDiff.Value);
                }
            }
            else if (!JToken.DeepEquals(prop.Value, sampleValue))
            {
                differences.Add(fullKey, new ValueDifference(prop.Value, sampleValue));
            }
        }

        foreach (var prop in sample.Properties())
        {
            string fullKey = string.IsNullOrEmpty(parentKey) ? prop.Name : $"{parentKey}.{prop.Name}";

            if (!reference.TryGetValue(prop.Name, out _))
            {
                differences.Add(fullKey, new ValueDifference(null, prop.Value));
            }
        }

        return differences;
    }

    class ValueDifference
    {
        public JToken ReferenceValue { get; }
        public JToken SampleValue { get; }

        public ValueDifference(JToken referenceValue, JToken sampleValue)
        {
            ReferenceValue = referenceValue;
            SampleValue = sampleValue;
        }

        public override string ToString()
        {
            return $"{ReferenceValue?.ToString() ?? "null"} != {SampleValue?.ToString() ?? "null"}";
        }
    }
}
