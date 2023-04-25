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
using System.Runtime.CompilerServices;
using Microsoft.SqlServer.Server;
using System.Collections;
using System.Globalization;

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

        private static DataTable GetSql()
        {
            DataTable result = new DataTable();
            using (SqlConnection conn = new SqlConnection(string.Format("Server=DBSQL;Database=Nordavia;Integrated Security=SSPI;")))
            {
                conn.Open();
                using (SqlCommand comm = new SqlCommand("select * from NpsCrew where Date>=@from and Date<=@to", conn))
                {
                    //5Н115 Flight
                    DateTime from = DateTime.Today.AddDays(-DateTime.Today.Day);
                    from = from.AddDays(-from.Day + 1);
                    comm.Parameters.AddWithValue("@from", from);
                    comm.Parameters.AddWithValue("@to", DateTime.Today.AddDays(-DateTime.Today.Day));
                    
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
            //XDocument xdoc = XDocument.Parse(Get("https://ref.flysmartavia.com/polls/xml.php?from=01.03.2023&to=31.03.2023&p=freREG3fGHRGRE435ggerg"));
            //XElement data = xdoc.Element("data");

            //var dicPax = new Dictionary<string, List<int>>();
            //var dicMain = new Dictionary<string, Dictionary<string, List<int>>>();

            //foreach (XElement person in data.Elements())
            //{

            //    XElement date = person.Element("date");
            //    XElement flight = person.Element("flight");
            //    string strKay = DateTime.Parse(date.Value).ToString() + flight.Value;
            //    XElement grade = person.Element("grade");
            //    XElement pnr = person.Element("pnr");
            //    if (!dicPax.ContainsKey(strKay))
            //        dicPax.Add(strKay, new List<int>());
            //    dicPax[strKay].Add(Convert.ToInt32(grade.Value));
            //}


            DataTable dtCrew = GetSql();

            foreach (DataRow row in dtCrew.Rows)
            {
                string mainKey = "";
                var cells = row.ItemArray;
                mainKey = ((DateTime)row["Date"]).ToString("MM-yyyy");
                Console.WriteLine(mainKey);

                    
                //if (!dicMain.ContainsKey(mainKey))
                //    dicMain.Add(mainKey, new Dictionary<string, List<int>>());
                //if (dicMain.ContainsKey(mainKey) && dicPax.ContainsKey(mainKey))
                //    if (dicPax.Contains)
                //        dicMain[mainKey].Add(tabNumKey, dicPax[mainKey]);
            }

            Console.ReadKey();




            //foreach (var keyMain in dicMain.Keys)
            //{
            //    Console.Write($"key {keyMain} tabNum:");
            //    foreach (var tabNumKey in dicMain[keyMain])
            //    {
            //        Console.Write($"{tabNumKey}\t");
            //        //foreach (var item in dicMain[keyMain][tabNumKey])
            //        //{
                        
            //        //}
            //    }
            //    Console.WriteLine();
            //}
            //Console.ReadKey();
        }
    }
}
