/*
 * Copyright SteveSmith.Software 2021. All Rights Reserverd.
 * 
 * Supplied under the MIT License
 */

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using Microsoft.Win32;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using System.Configuration;

namespace Clock
{
	public partial class Form1 : Form
	{
		bool dateset = false;

		HttpClient httpClient = null;
		HttpResponseMessage response = null;
		Uri baseAddress;
		string user;
		string pass;
		string responseData = null;
		dynamic resp;
		bool hasConnection=false;
		string id = string.Empty;
		float lastSoc = 0f;
		string socTrend = "";
		int itemCnt = 0;

		string language;
		Color textColor = Color.Goldenrod;

		enum types
		{
			soc,
			acIn,
			acOut,
			genSet,
			pv,
			COUNT
		}

		bool[] items = new bool[(int)types.COUNT];

		public Form1()
		{
			InitializeComponent();
			ShowInTaskbar = false;
			this.BackColor = Color.Magenta;
			label1.BackColor = Color.Magenta;
			label2.BackColor = Color.Magenta;
			label3.BackColor = Color.Magenta;
			this.TransparencyKey = Color.Magenta;
			notifyIcon1.Visible = true;
			SystemEvents.PowerModeChanged += OnPowerChange;

			int x = getConfigInt("positionX");
			int y = getConfigInt("positionY");
			Location = new Point(x, y);

			string color = getConfigString("colour").ToLower();

			string[] colors = Enum.GetNames(typeof(KnownColor));
			int[] colVals = (int[])Enum.GetValues(typeof(KnownColor));
			for (int i=0;i<colors.Length;i++)
			{
				if (color == colors[i].ToLower())
				{
					KnownColor kn = (KnownColor)colVals[i];
					textColor = Color.FromKnownColor(kn);
					break;
				}
			}

			label1.ForeColor = textColor;
			label2.ForeColor = textColor;
			label3.ForeColor = textColor;

			string url = "https://vrmapi.victronenergy.com/v2/";
			baseAddress = new Uri(url);
			string username = getConfigString("username"); 
			string password = getConfigString("password");

			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
			{
				label3.Text = "No Username or Password";
			}
			user = @"{ ""username"":""" + username + @""",";
			pass = @" ""password"":""" + password + @"""}";

			for (int i=0;i<(int)types.COUNT;i++)
			{
				items[i] = getConfigBool(((types)i).ToString());
				if (items[i]) itemCnt++;
			}

			language = getConfigString("lang");
			if (string.IsNullOrEmpty(language)) language = "en-US";

			timer1_Tick(null, null);
		}

		bool TryGetConnection()
		{
			try
			{
				httpClient = new HttpClient();
				httpClient.BaseAddress = baseAddress;
				System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
				System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

				HttpContent content = new StringContent(user + pass, Encoding.UTF8, "application/json");

				response = httpClient.PostAsync("auth/login", content).Result;
				responseData = response.Content.ReadAsStringAsync().Result;
				resp = JsonConvert.DeserializeObject<object>(responseData);
				id = resp["idUser"];
				string token = resp["token"];

				httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Authorization", "Bearer " + token);
				return true;
			}
			catch 
			{
				label3.Text = "No connection available";
			}
			if (httpClient != null) httpClient.Dispose();
			httpClient = null;
			return false;
		}

		void timer1_Tick(object sender, EventArgs e)
		{
			label2.Text = DateTime.Now.ToShortTimeString();
			label3.Text = GetData();
			if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0) dateset = false;
			if (!dateset)
			{
				label1.Text = DateTime.Now.ToString("D", CultureInfo.CreateSpecificCulture(language));
				dateset = true;
			}
			int miliseconds = 60000 - ((DateTime.Now.Second * 1000) + DateTime.Now.Millisecond);
			timer1.Interval = miliseconds;
		}

		void notifyIcon1_DoubleClick(object sender, EventArgs e)
		{
			notifyIcon1.Visible = false;
			SystemEvents.PowerModeChanged -= OnPowerChange;
			if (hasConnection) httpClient.Dispose();
			Close();
		}

		void notifyIcon1_Click(object sender, EventArgs e)
		{
			Visible = !Visible;
		}

		void OnPowerChange(object s, PowerModeChangedEventArgs e)
		{
			switch (e.Mode)
			{
				case PowerModes.Resume:
					dateset = false;
					timer1_Tick(null, null);
					break;
				case PowerModes.Suspend:
					httpClient.Dispose();
					httpClient = null;
					hasConnection = false;
					break;
			}
		}

		string GetData()
		{
			try
			{
				if (!hasConnection)
				{
					hasConnection = TryGetConnection();
				}

				if (hasConnection)
				{
					response = httpClient.GetAsync("users/" + id + "/installations?extended=1").Result;
					responseData = response.Content.ReadAsStringAsync().Result;
					resp = JsonConvert.DeserializeObject<object>(responseData);
					dynamic records = resp["records"];
					dynamic record = records[0];
					string siteId = record["idSite"];
					dynamic extended = record["extended"];

					string[] sa = new string[(int)types.COUNT];
					for (int i = 0; i < (int)types.COUNT; i++)
					{
						sa[i] = string.Empty;
					}

					int cnt = 0;
					bool finished = false;

					for (int i = 0; i < extended.Count; i++)
					{
						dynamic data = extended[i];

						string code = data["code"];
						string value = data["formattedValue"];

						switch (code)
						{
							case "bs":
								cnt++;
								if (!items[(int)types.soc]) break;
								float soc = data["rawValue"];
								if (lastSoc == 0f) lastSoc = soc;
								if (soc > lastSoc) socTrend = "\U00002191";
								else if (soc < lastSoc) socTrend = "\U00002193";
								lastSoc = soc;
								sa[(int)types.soc] = "SOC " + socTrend + value;
								if (soc < 15f)
								{
									label3.ForeColor = Color.Red;
								}
								else
								{
									if (soc > 98)
									{
										label3.ForeColor = Color.PowderBlue;
									}
									else
									{
										label3.ForeColor = textColor;
									}
								}
								if (cnt == itemCnt) finished = true;
								break;
							case "consumption":
								cnt++;
								if (!items[(int)types.acOut]) break;
								if (data["rawValue"] != "0") sa[(int)types.acOut] = "AcOut " + value;
								if (cnt == itemCnt) finished = true;
								break;
							case "ac_in":
								cnt++;
								if (!items[(int)types.acIn]) break;
								if (data["rawValue"] != "0") sa[(int)types.acOut] = "AcIn " + value;
								if (cnt == itemCnt) finished = true;
								break;
							case "solar_yield":
								cnt++;
								if (!items[(int)types.pv]) break;
								if (data["rawValue"] != "0") sa[(int)types.pv] = "PV " + value;
								if (cnt == itemCnt) finished = true;
								break;
							case "generator":
								cnt++;
								if (!items[(int)types.genSet]) break;
								if (data["rawValue"] == null) break;
								if (data["rawValue"] != "0") sa[(int)types.genSet] = "Genset " + value;
								if (cnt == itemCnt) finished = true;
								break;
						}
						if (finished) break;
					}

					string s = string.Join(" ", sa);
					return s;
				}
			}
			catch 
			{
			}
			return "No Data available";

		}

		internal string getConfigString(string token)
		{
			try
			{
				string text = ConfigurationManager.AppSettings[token];
				return text;
			}
			catch { }
			return string.Empty;
		}

		internal bool getConfigBool(string token)
		{
			try
			{
				string text = ConfigurationManager.AppSettings[token];
				bool n;
				bool ok = Boolean.TryParse(text, out n);
				if (ok)
				{
					return n;
				}
				else
				{
					Console.WriteLine("Invalid value in AppSettings for " + token);
					return false;
				}
			}
			catch { }
			return false;
		}

		internal int getConfigInt(string token)
		{
			try
			{
				string text = ConfigurationManager.AppSettings[token];
				int n;
				bool ok = int.TryParse(text, out n);
				if (ok)
				{
					return n;
				}
				else
				{
					Console.WriteLine("Invalid value in AppSettings for " + token);
					return 0;
				}
			}
			catch { }
			return -1;
		}
	}
}
