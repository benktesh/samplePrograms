using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /* *
 * This partial class fetches data from WorldBank Api asynchronously.
 * The risk parameters are passed on to a gridview and the risk parameters.
 * The risk values are populated on a UI.
 * The class requires active internet connection to work.
 * */
    public partial class RiskForm : System.Web.UI.Page
    {
        [DataContract]
        public class LendingType
        {
            [DataMember]
            String id { get; set; }
            [DataMember]
            String value { get; set; }
        };
        [DataContract]
        public class IncomeLevel
        {
            [DataMember]
            String id { get; set; }
            [DataMember]
            String value { get; set; }
        } ;
        [DataContract]
        public class Region
        {
            [DataMember]
            String id { get; set; }
            [DataMember]
            String region { get; set; }
        };
        [DataContract]
        public class AdminRegion
        {
            [DataMember]
            String id { get; set; }
            [DataMember]
            String region { get; set; }
        }

        [DataContract]
        public class Country
        {
            [DataMember]
            public String id { get; set; }
            [DataMember]
            public String iso2Code { get; set; }
            [DataMember]
            public String name { get; set; }
            [DataMember]
            public Region region { get; set; }
            [DataMember]
            public AdminRegion adminregion { get; set; }
            [DataMember]
            public IncomeLevel incomeLevel { get; set; }
            [DataMember]
            public LendingType lendingType { get; set; }
            [DataMember]
            public String capitalCity { get; set; }
            [DataMember]
            public String longitude { get; set; }
            [DataMember]
            public String latitude { get; set; }
        }

        [DataContract]
        public class Indicator
        {
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public string value { get; set; }
        }
        [DataContract]
        public class CountryIndicator
        {
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public string value { get; set; }
        }
        [DataContract]
        public class wgiIndicator
        {
            [DataMember]
            public Indicator indicator { get; set; }
            [DataMember]
            public CountryIndicator country { get; set; }
            [DataMember]
            public string value { get; set; }
            [DataMember]
            public string _decimal { get; set; }
            [DataMember]
            public string date { get; set; }
        }

        public static T DeserializeJSon<T>(string jsonString)
        {
            try
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
                T obj = (T)ser.ReadObject(stream);
                return obj;
            }
            catch (Exception e)
            {
                Debug.WriteLine("An exception occured while deserializing jsonString " + jsonString + e.Message);
                return default(T);
            }
        }

        protected async void bgGetWBData_Click(object sender, EventArgs e)
        {
            if (dropCountryList.SelectedIndex > 0)
            {
                var selectedCountryId = dropCountryList.SelectedValue;
                Debug.WriteLine("The selected country : " + dropCountryList.SelectedItem.Value);
                String[,] wgiIndicatorDefinition = new String[6, 3]
                {
                    {"CC.EST", "Corruption control", ""},                 
                    {"GE.EST", "Government Effectiveness", ""},
                    {"PV.EST",  "Political Stability and Absence of Violence/Terrorism" , ""},
                    {"RL.EST", "Rule of Law", ""},
                    {"RQ.EST", "Regulatory Quality", ""},
                    {"VA.EST", "Voice and Accountability", ""}
                };

                var wgiValues = new Dictionary<string, string[,]>();
                String selectedIndicator = "";

                for (int i = 0; i < 6; i++)
                {
                    Debug.WriteLine("Beginning fetch " + DateTime.Now);
                    selectedIndicator = wgiIndicatorDefinition[i, 0];
                    Debug.Write("Selected indicator is " + selectedIndicator);

                    StringBuilder s = new StringBuilder("http://api.worldbank.org/countries/" + selectedCountryId + "/indicators/" + selectedIndicator + "?source=3&format=json");
                    Debug.Write("The built URI is = " + s);

                    String getWgi = await RunAsync(s.ToString());

                    Debug.WriteLine("Got the result from api " + DateTime.Now);

                    if (getWgi.StartsWith("Error") || getWgi.StartsWith("ERROR") || getWgi.Equals("hello"))
                        Response.Write(getWgi);
                    else
                    {
                        var obj = DeserializeJSon<List<wgiIndicator>>(getWgi);
                        wgiValues.Add(selectedIndicator, getIndicatorValues(obj));
                    }
                }
                String[,] result = new String[8, 7];

                result[0, 0] = "Indicator/Year";
                result[7, 0] = "Average";
                result[0, 6] = "Average";
                int keyposition = 1;
                foreach (var key in wgiValues.Keys)
                {
                    result[keyposition, 0] = key.Substring(0, key.IndexOf("."));
                    keyposition++;
                }


                var keyCC = wgiValues["CC.EST"];
                String msg = "printing result";
                keyposition = 1;
                bool firstPass = true;
                foreach (var key in wgiValues.Keys)
                {
                    var value = wgiValues[key];
                    Debug.WriteLine(key + " : ");
                    for (int i = 0; i < 6; i++)
                    {
                        if (firstPass)
                        {
                            result[0, i + 1] = value[i, 0];
                        }
                        result[keyposition, i + 1] = value[i, 1];

                    }
                    keyposition = keyposition + 1;
                    firstPass = false;
                }

                for (int i = 1; i <= 6; i++)
                {
                    Double yearAverage = 0.00;
                    for (int j = 1; j <= 6; j++)
                    {
                        yearAverage = yearAverage + Convert.ToDouble(result[j, i]);
                        result[j, i] = Convert.ToString(Math.Round(Convert.ToDouble(result[j, i]), 2));
                    }
                    result[7, i] = Convert.ToString(Math.Round(yearAverage / 6, 2));
                }

                var datatable = new DataTable("dtWgiIndicator");

                for (int i = 0; i < 7; i++)
                {
                    datatable.Columns.Add(result[0, i]);
                }
                var row = new String[7];
                for (int j = 1; j < 8; j++)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        row[i] = (result[j, i]);

                    }
                    datatable.Rows.Add(row);
                }

                msg = format2DArray(result);
                Debug.Print(msg);
                GridView1.DataSource = datatable;
                GridView1.DataBind();

                wgiSelector(Convert.ToDouble(result[7, 6]));
            }
            else
            {
                this.GridView1.Visible = false;
                Response.Write("Please select a valid country");
            }
        }

       

        //Helper method to print a 2 day array used for debugging
        private String format2DArray(String[,] a)
        {
            //msg = "Test 2D array print : ";
            String msg = "";
            for (int i = 0; i <= a.GetUpperBound(0); i++)
            {
                msg = msg + "\n ";
                for (int x = 0; x <= a.GetUpperBound(1); x++)
                {
                    if (x == a.GetUpperBound(1))
                        msg = msg + a[i, x];
                    else
                        msg = msg + a[i, x] + ",";
                }
            }
            //Debug.WriteLine(msg);
            return msg;
        }

        //Make calculations for individual risk components.
        private static String[,] getIndicatorValues(List<wgiIndicator> obj)
        {
            String[,] indicatorValues = new String[6, 2];
            //The following code can be refactored to a funciton to get average
            int count = 0;
            Double val = 0.000;
            if (obj == null)
                return indicatorValues;

            foreach (wgiIndicator i in obj)
            {
                Debug.WriteLine("Working on indicator year " + count);
                if (i.value.Equals("null") || i.value == null)
                    continue;
                if (count <= 4)
                {
                    indicatorValues[count, 0] = i.date;
                    indicatorValues[count, 1] = i.value;
                    val = val + Double.Parse(i.value);
                    // Debug.WriteLine(count + " " + i.date + " " + i.value + "val is " + val);
                    count = count + 1;
                }
                else
                    break;
            }
            indicatorValues[5, 0] = "Average";
            indicatorValues[5, 1] = Convert.ToString(val / (count));

            return indicatorValues;
        }

        //This methods populates the risk components coming from wgi.
        private void wgiSelector(double x)
        {
            Debug.WriteLine("Checking the value of for radio selection " + x);
            if (x >= 0.82)
            {
                this.RadioButton_PR5.Checked = true;
                politicalRiskValue5.InnerText = "0";
                return;
            }
            else if (x >= 0.19)
            {
                this.RadioButton_PR4.Checked = true;
                politicalRiskValue4.InnerText = "1";
                return;
            }
            else if (x >= -0.32)
            {
                this.RadioButton_PR3.Checked = true;
                politicalRiskValue3.InnerText = "2";

                return;
            }
            else if (x >= -0.79)
            {
                this.RadioButton_PR2.Checked = true;
                politicalRiskValue2.InnerText = "4";
                return;
            }
            else
            {
                this.RadioButton_PR1.Checked = true;
                politicalRiskValue1.InnerText = "6";
                return;
            }

        }
        /*
         * This method returns a serialized data in json format for a webclient.
         * the method does not check the correctness of uri.
         * For any error, the returned string starts with "ERROR". This informaiton
         * can be used to provide appropriate message.
         * 
         */
        static async Task<String> RunAsync(String uri)
        {
            Debug.WriteLine("Processing Async");
            using (var client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Debug.WriteLine(uri);
                HttpResponseMessage response = await client.GetAsync(uri);

                String responseBody = "";
                if (response.IsSuccessStatusCode)
                {
                    responseBody = await response.Content.ReadAsStringAsync();
                    /* 
                     * The API is giving a page header. The following string parsing removes the extra string and makes string to
                     * comply with strict json format.
                     */
                    int x = responseBody.IndexOf(",[{\"");
                    int y = responseBody.LastIndexOf("]");
                    responseBody = responseBody.Substring(x + 1, y - x);
                    responseBody = responseBody.Replace("decimal", "_decmial");
                    if (responseBody.StartsWith("[{\"page\""))
                    {
                        responseBody = "ERROR - no data exist for selected location/indicator ";
                    }
                }
                else
                    responseBody = ("ERROR " + response.StatusCode.ToString());
                return responseBody;
            }
        }

    }
}