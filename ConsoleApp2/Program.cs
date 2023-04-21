using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConsoleApp2
{    
    internal class Program
    {
        private static string _sessionId;
        private static string Get(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            if (_sessionId != null)
                request.Headers.Add("X-SBISSessionID", _sessionId);
            try
            {
                using (WebResponse webResponse = request.GetResponse())
                using (Stream st = webResponse.GetResponseStream())
                {
                    string result;
                    using (StreamReader sr = new StreamReader(st, Encoding.UTF8))
                        result = sr.ReadToEnd();
                    return result;
                }
            }
            catch (WebException e)
            {
                using (var stream = e.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    throw new Exception(reader.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private static DataTable GetUserPlanWork()
        {
            DataTable result = new DataTable();
            using (SqlConnection conn = new SqlConnection(string.Format("Server=DBSQL;Database=Nordavia;Integrated Security=SSPI;")))
            {
                conn.Open();
                using (SqlCommand comm = new SqlCommand("select * from NpsCrew where Date>=@from and Date<=@to", conn))
                {
                    comm.Parameters.AddWithValue("@from", DateTime.Today.AddDays(-1));
                    comm.Parameters.AddWithValue("@to", DateTime.Today.AddDays(10));
                  
                    using (SqlDataReader sqlreader = comm.ExecuteReader())
                    {
                        result.Load(sqlreader);
                        for (int i = 0; i < result.Columns.Count - 1; i++)
                            result.Columns[i].ReadOnly = false;
                    }
                }
            }
            return result;
        }


        static void Main(string[] args)
        {

            XDocument xdoc = XDocument.Parse(Get("https://ref.flysmartavia.com/polls/xml.php?from=01.03.2023&to=31.03.2023&p=freREG3fGHRGRE435ggerg"));
            XElement data = xdoc.Element("data");
            foreach (XElement person in data.Elements("pax"))
            {

                XElement name = person.Element("date");
                XElement company = person.Element("flight");
                XElement grade = person.Element("grade");
                XElement pnr = person.Element("pnr");

                Console.WriteLine($"date: {name?.Value}");
                Console.WriteLine($"flight: {company?.Value}");
                Console.WriteLine($"grade: {grade?.Value}");
                Console.WriteLine($"pnr: {pnr?.Value}");

                Console.WriteLine();
            }
            Console.ReadLine();
        }
    }
}
