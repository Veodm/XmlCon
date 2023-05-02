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


        static double FindCoeff(DateTime date, double nps, DataTable dtCoeffs, int npsCount)
        {
            if (npsCount > 5)
            {
                foreach (DataRow row in dtCoeffs.Rows)
                {
                    DateTime dateFrom = (DateTime)row["DateFrom"];
                    DateTime dateTo = (DateTime)row["DateTo"];
                    double npsFrom = (double)row["From"];
                    double npsTo = (double)row["To"];
                    double kFrom = (double)row["CoeffFrom"];
                    double kTo = (double)row["CoeffTo"];

                    if (date >= dateFrom && date <= dateTo && nps >= npsFrom && nps <= npsTo)
                    {
                        double k = (kTo - kFrom) / (npsTo - npsFrom);
                        double b = kFrom - k * npsFrom;
                        return k * nps + b;
                    }
                }

            }
            return 1;
        }

        static DateTime GetDateKey(DateTime date)
        {
            return date.AddDays(-date.Day + 1);
        }

        static void Main(string[] args)
        {
            XDocument xdoc = XDocument.Parse(Get("https://ref.flysmartavia.com/polls/xml.php?from=01.03.2023&to=31.03.2023&p=freREG3fGHRGRE435ggerg"));
            XElement data = xdoc.Element("data");

            var dicPax = new Dictionary<string, List<int>>();
            var dicMain = new Dictionary<DateTime, Dictionary<string, List<int>>>();
            var dicCoeff = new Dictionary<DateTime, Dictionary<string, List<double[]>>>();
            var dicRes = new Dictionary<DateTime, List<double[]>>();

            DataTable dtPar = new DataTable();
            dtPar.Columns.Add("From");
            dtPar.Columns.Add("To");
            dtPar.Columns.Add("CoeffFrom");
            dtPar.Columns.Add("CoeffTo");
            dtPar.Columns.Add("DateFrom");
            dtPar.Columns.Add("DateTo");

            dtPar.Rows.Add(0, 5, 1, 1, new DateTime(2021, 11, 01), new DateTime(2022, 01, 31));
            dtPar.Rows.Add(0, 1.5, 0, 0, new DateTime(2022, 02, 01), new DateTime(2030, 01, 01));
            dtPar.Rows.Add(1.5, 3.5, 0, 0.5, new DateTime(2022, 02, 01), new DateTime(2030, 01, 01));
            dtPar.Rows.Add(3.5, 5.5, 0.5, 1, new DateTime(2022, 02, 01), new DateTime(2030, 01, 01));
            dtPar.Rows.Add(5.5, 6, 1, 1, new DateTime(2022, 02, 01), new DateTime(2030, 01, 01));
            dtPar.Rows.Add(6, 7, 1, 1.25, new DateTime(2022, 02, 01), new DateTime(2030, 01, 01));

            foreach (XElement person in data.Elements())
            {

                XElement date = person.Element("date");
                XElement flight = person.Element("flight");
                string strKey = DateTime.Parse(date.Value).ToString() + flight.Value;
                XElement grade = person.Element("grade");
                XElement pnr = person.Element("pnr");
                if (!dicPax.ContainsKey(strKey))
                    dicPax.Add(strKey, new List<int>());
                dicPax[strKey].Add(Convert.ToInt32(grade.Value));
            }

            DataTable dtCrew = GetSql();
            foreach (DataRow rowCrew in dtCrew.Rows)
            {
                var mainKey = GetDateKey((DateTime)rowCrew["Date"]);
                string dateFlightKey = mainKey + rowCrew["flight"].ToString().Substring(2);
                string tabKey = rowCrew["CrewTabNum"].ToString();

                if (!dicMain.ContainsKey(mainKey))
                    dicMain.Add(mainKey, new Dictionary<string, List<int>>());

                if (!dicMain[mainKey].ContainsKey(tabKey))
                    dicMain[mainKey].Add(tabKey, new List<int>());

                if (dicPax.ContainsKey(dateFlightKey))
                    foreach (var grade in dicPax[dateFlightKey])
                        dicMain[mainKey][tabKey].Add(grade);
            }
            
            foreach (var period in dicMain.Keys)
            {
                foreach (var tabNum in dicMain[period].Keys)
                {
                    double nps = 0;
                    int npsCount = 0;

                    foreach (var grade in dicMain[period][tabNum])
                    {
                        nps += grade;
                        npsCount++;
                    }
                    nps /= npsCount;

                    double coeff = FindCoeff(period, nps, dtPar, npsCount);

                    if (!dicCoeff.ContainsKey(period))
                        dicCoeff.Add(period, new Dictionary<string, List<double[]>>());

                    if (!dicCoeff[period].ContainsKey(tabNum))
                    {
                        dicCoeff[period].Add(tabNum, new List<double[]>());
                        dicCoeff[period][tabNum].Add(new double[] { nps, coeff });
                    }
                }
            }
            
            foreach (var period  in dicCoeff.Keys)
            {
                int quantity = 0;
                double npsAll = 0;
                double coeffAll = 0;
                int coeffMore1 = 0;
                int coeff1 = 0;
                int coeffLess1 = 0;
                foreach (var tabNum in dicCoeff[period].Keys)
                {
                    foreach(var grade in dicCoeff[period][tabNum])
                    {
                        npsAll += grade[0];
                        coeffAll += grade[1];                        
                        if (grade[1] > 0)
                            coeffMore1++;
                        else if (grade[1] < 0)
                            coeffLess1++;
                        else
                            coeff1++;
                    }
                    quantity++;
                }
                if(!dicRes.ContainsKey(period))
                {
                    npsAll /= quantity;
                    coeffAll /= quantity;
                    dicRes.Add(period,new List<double[]>());
                    dicRes[period].Add(new double[] {npsAll, coeffAll,coeffMore1,coeff1,coeffLess1});
                }
                
            }

            Console.ReadLine();
        }

        
    }
}