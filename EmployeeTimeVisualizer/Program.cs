using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

class TimeEntry
{
    public string EmployeeName { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
}


class EmployeeTime
{
    public string Name { get; set; }
    public int TotalTime { get; set; }
}

class Program
{
    const string API_URL = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";

    static void Main()
    {
        var entries = FetchData();
        var employeeTimes = AggregateTime(entries);

        GenerateHtml(employeeTimes);
        GeneratePieChart(employeeTimes);

        Console.WriteLine("Done! Check output folder for HTML and PNG.");
        Console.ReadLine();
    }

    static List<TimeEntry> FetchData()
    {
        using (var client = new HttpClient())
        {
            var response = client.GetAsync(API_URL).Result;

            Console.WriteLine("Response Status: " + response.StatusCode);

            var json = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Raw JSON response:");
            Console.WriteLine(json.Substring(0, Math.Min(json.Length, 500))); // Print first 500 chars

            return JsonConvert.DeserializeObject<List<TimeEntry>>(json);
        }
    }

    static List<EmployeeTime> AggregateTime(List<TimeEntry> entries)
    {
        return entries
            .Where(e => e.EndTimeUtc > e.StartTimeUtc) // valid time entries
            .GroupBy(e => e.EmployeeName)
            .Select(g => new EmployeeTime
            {
                Name = g.Key,
                TotalTime = g.Sum(e =>
                    (int)(e.EndTimeUtc - e.StartTimeUtc).TotalHours)
            })
            .OrderByDescending(e => e.TotalTime)
            .ToList();
    }


    static void GenerateHtml(List<EmployeeTime> employeeTimes)
    {
        string html = @"<html><head><title>Employee Time</title></head><body>" +
                      "<h2>Employee Time Report</h2><table border='1' cellpadding='5'><tr><th>Name</th><th>Total Time Worked</th></tr>";

        foreach (var emp in employeeTimes)
        {
            string color = emp.TotalTime < 100 ? " style='background-color: #f99;'" : "";
            html += $"<tr{color}><td>{emp.Name}</td><td>{emp.TotalTime}</td></tr>";
        }

        html += "</table></body></html>";
        File.WriteAllText("employee_time.html", html);
    }

    static void GeneratePieChart(List<EmployeeTime> employeeTimes)
    {
        int width = 600, height = 600;
        Bitmap bmp = new Bitmap(width, height);
        Graphics g = Graphics.FromImage(bmp);
        g.Clear(Color.White);

        double total = employeeTimes.Sum(e => (double)e.TotalTime);
        float angleStart = 0;
        Random rnd = new Random();

        foreach (var emp in employeeTimes)
        {
            float sweep = (float)(emp.TotalTime / total * 360);
            Brush brush = new SolidBrush(Color.FromArgb(rnd.Next(100, 255), rnd.Next(100, 255), rnd.Next(100, 255)));
            g.FillPie(brush, 100, 100, 400, 400, angleStart, sweep);
            angleStart += sweep;
        }


        bmp.Save("employee_piechart.png", ImageFormat.Png);
    }
}
