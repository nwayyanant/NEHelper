using Newtonsoft.Json;
using System.Text.RegularExpressions;

class Program
{
    public static string PageMarker = "";
    public static string PaliMarker = "";
    public static string TranslationMarker = "";
    public static string FootnoteMarker = "";
    public static string RemarkMarker = "";
    public static string ParagraphMarker = "";

    public static int StartLine = 0;
    public static int EndLine = 0;

    public static string PaliWordStart = "";
    public static string PaliWordEnd = "";
    public static string TranslationWordStart = "";
    public static string TranslationWordEnd = "";
    public static string FootnoteWordStart = "";
    public static string FootnoteWordEnd = "";

    public static string DocNo = "";
    static void Main()
    {

        var MainFolderPath = Environment.CurrentDirectory;
        var NeFolderPath = MainFolderPath + "\\ne\\";
        var fullHtmlOutputFolderPath = MainFolderPath +"\\html\\";
        var jsonFolderPath = MainFolderPath + "\\json\\";
        var htmlBodyFolderPath = MainFolderPath + "\\htmlbody\\";

        var configFile = "config.json";
        var error = String.Empty;
        try
        {
            ReadConfig(configFile);

            string[] neFiles = Directory.GetFiles(NeFolderPath);
            foreach (string neFile in neFiles)
            {
                var fileName = neFile.Split("\\").Last().Split(".").First();
                var neFilePath = NeFolderPath + fileName +".txt";
                var jsonFilePath = jsonFolderPath + fileName + ".json";
                var htmlBodyFilePath = htmlBodyFolderPath + fileName + ".html";
                var htmlRawFile = htmlBodyFolderPath + fileName +"_raw" + ".html";
                var fullHtmlOutputFilePath = fullHtmlOutputFolderPath + fileName + ".html";
                
                Console.WriteLine($"Reading header in NE file: {fileName} and processing to construct raw html...");
                var rawHtml = TransformTemplateToRawHtml(neFilePath, fullHtmlOutputFilePath);

                Console.WriteLine($"Create new html without NE data..");
                var newHtml = ReturnHeaderAndFooter(rawHtml);
                
                Console.WriteLine($"Validating {fileName}.txt and convert to json format...");
                
                if (!ConvertNeToJson(neFilePath, jsonFilePath))
                {
                    error = $"NE file {fileName} is not in correct format";
                    Console.WriteLine(error);
                    break;
                }

                Console.WriteLine($"Constructing html body...");
                var htmlBody = ConvertJsonToHtmlBody(jsonFilePath, htmlBodyFilePath);

                var fullHtml = newHtml.Header + htmlBody + newHtml.Footer;

                Console.WriteLine($"html file is saving as.. {fullHtmlOutputFilePath}");
                using (var writer = new StreamWriter(fullHtmlOutputFilePath))
                {
                    writer.AutoFlush = true;
                    writer.Write(fullHtml);
                }

                Console.WriteLine($"{fileName}.txt is saved as {fileName}.html");
            }

            if (error.Length !=0)
            {
                Console.WriteLine("Conversion Failed!");
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static bool ConvertNeToJson(string txtFilePath, string jsonFilePath)
    {
        // Read the content of the text file
        string content = string.Join("", File.ReadAllText(txtFilePath).Skip(1));
        content = content.Replace($"{ParagraphMarker}", "P").Replace($"{PageMarker}", "; #");

        string[] pages = content.Split(';');

        List<Dictionary<string, dynamic>> result = new List<Dictionary<string, dynamic>>();
        foreach (var page in pages)
        {
            // Split paragraphs based on 'P'
            string[] paragraphs = page.Split('P');

            

            var pos = -1;
            foreach (string paragraph in paragraphs)
            {
                // Skip empty paragraphs
                if (string.IsNullOrWhiteSpace(paragraph))
                    continue;
                List<Dictionary<string, string>> entries = new List<Dictionary<string, string>>();
                var aParagraph = new Dictionary<string, dynamic>();
                aParagraph.Add("startP","<p>");
                string[] words = paragraph.Split(" ");
                var pgNo = "";
                var pali = "";
                var trans = "";
                var myanFootnote = "";
                var remark = "";
                // Construct the dictionary for each entry

                foreach (var word in words)
                {   
                    if (Regex.Match(word, $@"{PageMarker}(\d+)").Success)
                    {
                        pgNo = word.Replace($"{PageMarker}","");
                        aParagraph.Add("pgNo", pgNo);
                        Console.WriteLine($"Scanning file " + txtFilePath.Split("\\").Last().Split(".").First() + $".txt. Reading and validating Page Number: {pgNo}");
                        if (pos > 0 && (pali.Trim().Length > 0 && trans.Trim().Length > 0))
                        {
                            var entry = new Dictionary<string, string>();
                            entry.Add("pali", pali);
                            entry.Add("translation", trans);

                            if (myanFootnote != "") entry.Add("myan_footnote", myanFootnote);
                            if (remark != "") entry.Add("myan_remark", remark); // Placeholder for remark, adjust as needed

                            entries.Add(entry);
                            aParagraph.Add("text", entries);

                            pali = "";
                            trans = "";
                            myanFootnote = "";
                            remark = "";
                        }

                        pos = 0;
                    }
                    else if (Regex.Match(word, $@"\{PaliMarker}(.*?)").Success)
                    { 
                        if (pos == 1 && trans.Length == 0) 
                        {
                            Console.WriteLine($"Translation {trans} text is empty. Error converting Page No: {pgNo}");
                            return false;
                        }
                        if (pos > 1 && (pali.Trim().Length >0 && trans.Trim().Length >0)) {
                            var entry = new Dictionary<string, string>();
                            entry.Add("pali", pali);
                            entry.Add("translation", trans);

                            if (myanFootnote != "") entry.Add("myan_footnote", myanFootnote);
                            if (remark != "") entry.Add("myan_remark", remark); // Placeholder for remark, adjust as needed

                            entries.Add(entry);
                            
                        }
                        pali = word.Replace($"{PaliMarker}", "");

                        pos = 1;
                    }
                    else if (Regex.Match(word, $@"\{TranslationMarker}(.*?)").Success)
                    {
                        if (pos >= 1 && pali.Length == 0)
                        {
                            Console.WriteLine($"Pali {pali} text is empty. Error converting Page No: {pgNo}");
                            return false;
                        }
                        trans = word.Replace($"{TranslationMarker}", "");
                        pos = 2;
                    }
                    else if (Regex.Match(word, $@"\{FootnoteMarker}(.*?)").Success)
                    {
                        myanFootnote = word.Replace($"{FootnoteMarker}","");
                        pos = 3;
                    }
                    else if (Regex.Match(word, $@"\{RemarkMarker}(.*?)").Success)
                    {
                        remark = word.Replace($"{RemarkMarker}","");
                        pos = 4;
                    }
                    else
                    {
                        switch (pos)
                        {
                            case 1:
                                pali = pali + " " + word;
                                break;
                            case 2:
                                trans = trans + " " + word;
                                break;
                            case 3:
                                myanFootnote = myanFootnote + " " + word;
                                break;
                            case 4:
                                remark = remark + " " + word;
                                break;

                        }
                    }
                }

                if ((pali.Trim().Length != 0 && trans.Trim().Length!= 0) || (myanFootnote != "") || (remark != ""))
                {
                    var entry = new Dictionary<string, string>();
                    if (pali != "") entry.Add("pali", pali);
                    if (trans != "") entry.Add("translation", trans);

                    if (myanFootnote != "") entry.Add("myan_footnote", myanFootnote);
                    if (remark != "") entry.Add("myan_remark", remark); // Placeholder for remark, adjust as needed

                    entries.Add(entry);
                    aParagraph.Add("text", entries);
                }

                aParagraph.Add("endP", "</p>");
                result.Add(aParagraph);
            }
        }

        using (StreamWriter writer = new StreamWriter(jsonFilePath))
        {
            // Convert to JSON and print or save the result
            string jsonResult = JsonConvert.SerializeObject(result, Formatting.Indented);

            // Write the HTML content to the output file
            writer.Write(jsonResult);
        }

        Console.WriteLine(txtFilePath.Split("\\").Last().Split(".").First() + ".txt is verified.");
        return true;
    }

    static string ConvertJsonToHtmlBody(string jsonFilePath, string htmlFilePath)
    {
        // Read JSON content from the file
        string jsonContent = System.IO.File.ReadAllText(jsonFilePath);

        var html = "";

        // Deserialize JSON into a dynamic object
        dynamic jsonFile = JsonConvert.DeserializeObject(jsonContent) ?? throw new InvalidOperationException();

        foreach (var jsonData in jsonFile)
        {
            // Start building HTML
            if ( jsonData.startP !=null) html += $"<p class=\"myan_indent\">{Environment.NewLine}";

            // Add page number
            if(jsonData.pgNo != null) html += $"\t<h>{jsonData.pgNo}</h>{Environment.NewLine}";

            if (jsonData.text != null)
            {
                // Add text entries
                foreach (var textEntry in jsonData.text)
                {
                    html +=
                        $"\t<span class=\"pali\">{textEntry.pali}</span><span class=\"trans\">{TranslationWordStart}{textEntry.translation}{TranslationWordEnd}</span>";
                    if (textEntry.myan_footnote != null)
                        html += $"\t<span class=\"myan_footnote\">{FootnoteWordStart}{textEntry.myan_footnote}{FootnoteWordEnd}</span>";
                    //if (textEntry.myan_remark != null)
                    //    html += $"\t<span class=\"myan_footnote\">{textEntry.myan_remark}</span>";

                    html += Environment.NewLine;
                }
            }

            // End HTML
            if (jsonData.endP != null)  html += $"</p>{Environment.NewLine}";
        }

        // Create a StreamWriter to write to the output HTML file
        using var writer = new StreamWriter(htmlFilePath);
        // Write the HTML content to the output file
        writer.Write(html);
        Console.WriteLine($"Saving NE only html text under : {htmlFilePath}");
        return html;
    }

    static string TransformTemplateToRawHtml(string inputFilePath, string outputFilePath)
    {
        // Read the template HTML file
        string template = File.ReadAllText("template.html");

        // Read all lines from the input text file
        string[] lines = File.ReadAllLines(inputFilePath);

        // Parse the JSON string in the first line to a dictionary
        Dictionary<string, string> parameters = ParseJsonParameters(lines[0]);

        // Create a StreamWriter to write to the output HTML file
        using StreamWriter writer = new StreamWriter(outputFilePath);
        // Replace parameters in the template
        var htmlContent = ReplaceParameters(template, parameters);

        // Write the HTML content to the output file
        //writer.Write(htmlContent);

        return htmlContent;
    }

    static string ReplacePlaceholders(string line, string template)
    {
        // Define regex patterns for each placeholder
        string paliPattern = @"\*(.*?)\^";
        string transPattern = @"\^(.*?)@";
        string footnotePattern = @"@(.*?)!";
        string remarkPattern = @"!(.*?)$";

        // Replace placeholders with corresponding values
        string htmlLine = template
            .Replace("{pali}", Regex.Match(line, paliPattern).Groups[1].Value)
            .Replace("{trans}", Regex.Match(line, transPattern).Groups[1].Value)
            .Replace("{footnote}", Regex.Match(line, footnotePattern).Groups[1].Value)
            .Replace("{remark}", Regex.Match(line, remarkPattern).Groups[1].Value);

        return htmlLine;
    }
    static Dictionary<string, string> ParseJsonParameters(string jsonString)
    {
        try
        {
            // Attempt to deserialize the JSON string into a dictionary
            var parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

            return parameters;
        }
        catch (System.Text.Json.JsonException exception)
        {
            // If JSON parsing fails, return a default set of parameters or handle the error as needed
            return new Dictionary<string, string>();
        }
    }

    static string ReplaceParameters(string template, Dictionary<string, string> parameters)
    {
        // Replace parameters in the template
        foreach (var parameter in parameters)
        {
            string placeholder = $"%{parameter.Key.ToUpper()}%";
            template = template.Replace(placeholder, parameter.Value);
            if (parameter.Key == "DocNo")
            {
                DocNo = parameter.Value;
            }
        }

        return template;
    }

    static (string Header, string Footer) ReturnHeaderAndFooter(string template)
    {
        var header = "";
        var footer = "";
        int lineno = 0;

        string[] lines = template.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.None);
        foreach (var line in lines)
        {
            lineno += 1;
            if (lineno > EndLine) footer += line + Environment.NewLine;
            else if (lineno < StartLine) header += line + Environment.NewLine;
        }

        return (header, footer);
    }
    private static void ReadConfig(string configFile)
    {
        string configContent = System.IO.File.ReadAllText(configFile);

        dynamic jsonFile = JsonConvert.DeserializeObject(configContent) ?? throw new BadImageFormatException();
        if (jsonFile.markers != null)
        {
            var markers = jsonFile.markers;
            PageMarker = markers.page;
            PaliMarker = markers.pali;
            TranslationMarker = markers.translation;
            FootnoteMarker = markers.footnote;
            RemarkMarker = markers.remark;
            ParagraphMarker = markers.paragraph;
        }

        if (jsonFile.html_body != null)
        {
            var html_body = jsonFile.html_body;
            StartLine = Convert.ToInt32(html_body.startLine);
            EndLine = Convert.ToInt32(html_body.endLine);
        }

        if (jsonFile.enclosed_chars != null)
        {
            var enclosedchars = jsonFile.enclosed_chars;
            PaliWordStart = enclosedchars.pali_start;
            PaliWordEnd = enclosedchars.pali_end;
            TranslationWordStart = enclosedchars.translation_start;
            TranslationWordEnd = enclosedchars.translation_end;
            FootnoteWordStart = enclosedchars.footnote_start;
            FootnoteWordEnd = enclosedchars.footnote_end;

        }

        if (jsonFile.nefile_header != null)
        {
            var nefileheader = jsonFile.nefile_header;
            DocNo = nefileheader.DocNo;
        }

    }
}
