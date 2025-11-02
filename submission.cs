using System;
using System.Xml.Schema;
using System.Xml;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Linq;

/**
 * This template file is created for ASU CSE445 Distributed SW Dev Assignment 4.
 * Please do not modify or delete any existing class/variable/method names. However, you can add more variables and functions.
 * Uploading this file directly will not pass the autograder's compilation check, resulting in a grade of 0.
 **/

namespace ConsoleApp1
{
    public class Program
    {
        public static string xmlURL = "https://raw.githubusercontent.com/IvanB04/cse445-assignmnet4/refs/heads/main/Hotels.xml";
        public static string xmlErrorURL = "https://raw.githubusercontent.com/IvanB04/cse445-assignmnet4/refs/heads/main/HotelsErrors.xml";
        public static string xsdURL = "https://raw.githubusercontent.com/IvanB04/cse445-assignmnet4/refs/heads/main/Hotels.xsd";

        public static void Main(string[] args)
        {
            // Test 1: Verify valid XML
            
            string result = Verification(xmlURL, xsdURL);
            Console.WriteLine(result);
            Console.WriteLine();

            // Test 2: Verify XML with errors
            
            result = Verification(xmlErrorURL, xsdURL);
            Console.WriteLine(result);
            Console.WriteLine();

            // Test 3: Convert XML to JSON
            
            result = Xml2Json(xmlURL);
            Console.WriteLine(result);
        }

        // Q2.1 - Validates XML against XSD schema
        public static string Verification(string xmlUrl, string xsdUrl)
        {
            try
            {
                // Download the XSD schema
                string xsdContent = DownloadFile(xsdUrl);
                
                // Download the XML file
                string xmlContent = DownloadFile(xmlUrl);

                // Create XmlReaderSettings with schema validation
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;
                
                // Add the schema
                using (StringReader xsdReader = new StringReader(xsdContent))
                {
                    XmlSchema schema = XmlSchema.Read(xsdReader, null);
                    settings.Schemas.Add(schema);
                }

                // Variable to capture validation errors
                StringBuilder errorMessages = new StringBuilder();
                bool hasErrors = false;

                // Set up validation event handler
                settings.ValidationEventHandler += (sender, e) =>
                {
                    hasErrors = true;
                    if (errorMessages.Length > 0)
                    {
                        errorMessages.AppendLine(); // Add newline between errors
                    }
                    errorMessages.Append($"Validation Error: {e.Message}");
                    if (e.Exception != null)
                    {
                        errorMessages.AppendLine();
                        errorMessages.Append($"Line: {e.Exception.LineNumber}, Position: {e.Exception.LinePosition}");
                    }
                };

                // Validate the XML
                using (StringReader xmlReader = new StringReader(xmlContent))
                using (XmlReader reader = XmlReader.Create(xmlReader, settings))
                {
                    while (reader.Read()) { }
                }

                // Return result
                if (hasErrors)
                {
                    return errorMessages.ToString();
                }
                else
                {
                    return "No errors are found";
                }
            }
            catch (Exception ex)
            {
                return $"Validation Error: {ex.Message}";
            }
        }

        // Q2.2 - Converts XML to JSON format
        public static string Xml2Json(string xmlUrl)
        {
            try
            {
                // Download the XML file
                string xmlContent = DownloadFile(xmlUrl);

                // Load XML document
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);

                // Create JSON structure manually to match required format
                StringBuilder jsonBuilder = new StringBuilder();
                jsonBuilder.AppendLine("{");
                jsonBuilder.AppendLine("  \"Hotels\": {");
                jsonBuilder.AppendLine("    \"Hotel\": [");

                XmlNodeList hotelNodes = xmlDoc.SelectNodes("/Hotels/Hotel");
                
                for (int i = 0; i < hotelNodes.Count; i++)
                {
                    XmlNode hotel = hotelNodes[i];
                    jsonBuilder.AppendLine("      {");

                    // Name
                    string name = hotel.SelectSingleNode("Name")?.InnerText;
                    jsonBuilder.AppendLine($"        \"Name\": \"{EscapeJson(name)}\",");

                    // Phone numbers (array)
                    XmlNodeList phoneNodes = hotel.SelectNodes("Phone");
                    jsonBuilder.AppendLine("        \"Phone\": [");
                    for (int j = 0; j < phoneNodes.Count; j++)
                    {
                        string phone = phoneNodes[j].InnerText;
                        jsonBuilder.Append($"          \"{EscapeJson(phone)}\"");
                        if (j < phoneNodes.Count - 1)
                            jsonBuilder.AppendLine(",");
                        else
                            jsonBuilder.AppendLine();
                    }
                    jsonBuilder.AppendLine("        ],");

                    // Address
                    XmlNode addressNode = hotel.SelectSingleNode("Address");
                    if (addressNode != null)
                    {
                        jsonBuilder.AppendLine("        \"Address\": {");
                        
                        string number = addressNode.SelectSingleNode("Number")?.InnerText;
                        jsonBuilder.AppendLine($"          \"Number\": \"{EscapeJson(number)}\",");
                        
                        string street = addressNode.SelectSingleNode("Street")?.InnerText;
                        jsonBuilder.AppendLine($"          \"Street\": \"{EscapeJson(street)}\",");
                        
                        string city = addressNode.SelectSingleNode("City")?.InnerText;
                        jsonBuilder.AppendLine($"          \"City\": \"{EscapeJson(city)}\",");
                        
                        string state = addressNode.SelectSingleNode("State")?.InnerText;
                        jsonBuilder.AppendLine($"          \"State\": \"{EscapeJson(state)}\",");
                        
                        string zip = addressNode.SelectSingleNode("Zip")?.InnerText;
                        jsonBuilder.AppendLine($"          \"Zip\": \"{EscapeJson(zip)}\",");
                        
                        string airport = addressNode.SelectSingleNode("NearestAirport")?.InnerText;
                        jsonBuilder.AppendLine($"          \"NearestAirport\": \"{EscapeJson(airport)}\"");
                        
                        jsonBuilder.Append("        }");
                    }

                    // Rating attribute (optional)
                    XmlAttribute ratingAttr = hotel.Attributes["Rating"];
                    if (ratingAttr != null)
                    {
                        jsonBuilder.AppendLine(",");
                        jsonBuilder.Append($"        \"_Rating\": \"{EscapeJson(ratingAttr.Value)}\"");
                    }
                    else
                    {
                        jsonBuilder.AppendLine();
                    }

                    jsonBuilder.Append("      }");
                    if (i < hotelNodes.Count - 1)
                        jsonBuilder.AppendLine(",");
                    else
                        jsonBuilder.AppendLine();
                }

                jsonBuilder.AppendLine("    ]");
                jsonBuilder.AppendLine("  }");
                jsonBuilder.Append("}");

                return jsonBuilder.ToString();
            }
            catch (Exception ex)
            {
                return $"Error converting XML to JSON: {ex.Message}";
            }
        }

        // Helper method to download file from URL
        private static string DownloadFile(string url)
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }

        // Helper method to escape special characters in JSON strings
        private static string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            
            return text.Replace("\\", "\\\\")
                       .Replace("\"", "\\\"")
                       .Replace("\n", "\\n")
                       .Replace("\r", "\\r")
                       .Replace("\t", "\\t");
        }
    }
}