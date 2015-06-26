using System;
using MySql.Data;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using System.Net.NetworkInformation;
using System.Net;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text.RegularExpressions;

namespace HPPageCounter
{
	class MainClass
	{
		//Classes for the lists
		public class Printer{
			public string DEP { get; set; }
			public string SN { get; set; }
			public string IP { get; set; }
			public bool Active { get; set; }
//			public int PrinterWebType { get; set; }
			public int PageCounterMono { get; set; }
			public int PageCounterColor { get; set; }
		}
		public class NewInfos{
			public string SN { get; set; }
			public int PageCounterMono { get; set; }
			public int PageCounterColor { get; set; }
			public bool Active { get; set; }
		}
		public class ExportData{
			public string DEP { get; set; }
			public string SN { get; set; }
			public int PageCounterMono { get; set; }
			public int PageCounterColor { get; set; }
			public int MonthlyPagesMono { get; set; }
			public int MonthlyPagesColor { get; set; }
			public bool Connected { get; set; }
			public bool NewSN { get; set; }
		}
		//Classes for the lists END

		public static void Main (string[] args)
		{
			//Creating variables
			bool bDebug = true;
			bool bDBconnect = false;
			string sDBconnection = "";
			var Printerlist = new List<Printer> ();
			var NewDatalist = new List<NewInfos> ();
			var Exportlist = new List <ExportData> ();
			Ping pConnected = new Ping ();
			IPAddress ipConnected;
			MySql.Data.MySqlClient.MySqlConnection sqlConnection = new MySql.Data.MySqlClient.MySqlConnection (sDBconnection);
			//Creating variables END

			//establish MySQL Connection
			if (bDBconnect) {
				System.Console.WriteLine ("Opening DB connection.");
				try {
					sqlConnection.Open ();
				} catch (MySql.Data.MySqlClient.MySqlException ex) {
					if (bDebug) {
						System.Console.WriteLine ("Program stopped because of a critical error: Failed connecting to the Database!");
						System.Console.WriteLine (ex.Message);
						System.Console.ReadLine ();
						return;
					} else {
						//TODO: Logs schreiben
						return;
					}
				}
			}else{
				System.Console.WriteLine ("DB connection disabled.");
			}
			//establish MySQL Connection END

			//reading tables
			if (bDBconnect){
				System.Console.WriteLine ("Reading Printer list.");
			}else{
				System.Console.WriteLine ("No DB connection, creating test printers.");
				Printerlist.Add (new Printer {
					DEP = "DEP06743",
					SN = "DXXXS",
					IP = "127.0.0.1",
					Active = true,
//					PrinterWebType = 2,
					PageCounterMono = 0,
					PageCounterColor = 0
				});
//				Printerlist.Add (new Printer {
//					DEP = "DEP06743",
//					SN = "CXXXS",
//					IP = "127.0.0.1",
//					Active = true,
//					PrinterWebType = 1,
//					PageCounterMono = 0,
//					PageCounterColor = 0
//				});
			}
			//reading tables END

			//Query printers
			for (int i=0; i < Printerlist.Count; i++)
			{
				//Check if IP is valid
				if (IPAddress.TryParse (Printerlist [i].IP, out ipConnected)) {
					if (bDebug && !bDBconnect) {
						System.Console.WriteLine ("Valid IP Adress.");
					}
				}else{
					if (bDebug){
						System.Console.WriteLine ("Invalid IP Adress: " + Printerlist [i].DEP + " " + Printerlist [i].IP);
					}else{
						//TODO: Log schreiben
					}
					continue;
				}

				//Check if printer is online
				if (bDebug && !bDBconnect) {
					System.Console.WriteLine ("Pinging printer.");
				}
				if (pConnected.Send (ipConnected).Status == IPStatus.Success) {
					if (bDebug) {
						System.Console.WriteLine ("Host answered.");
					}
				}else{
					if(bDebug){
						System.Console.WriteLine ("Host unreachable!");
					}else if(Printerlist[i].Active == true){
						//TODO: Log schreiben
						NewDatalist.Add (new NewInfos {
							SN = Printerlist [i].SN,
							PageCounterMono = Printerlist [i].PageCounterMono,
							PageCounterColor = Printerlist [i].PageCounterColor,
							Active = false
						});
					}else{
						NewDatalist.Add (new NewInfos {
							SN = Printerlist [i].SN,
							PageCounterMono = Printerlist [i].PageCounterMono,
							PageCounterColor = Printerlist [i].PageCounterColor,
							Active = false
						});
					}
					continue;
				}

				//Quering printer and writing the results in a list
				NewDatalist.Add (QueryPrinter (Printerlist [i].IP, bDebug));
			}
			//Query printers END

			if(bDebug){
				for (int i=0; i < NewDatalist.Count; i++){
					System.Console.WriteLine (NewDatalist [i].SN + " " + NewDatalist [i].PageCounterMono + " " + NewDatalist [i].PageCounterColor + " " + NewDatalist [i].Active);
				}
			}

			//Write infos into ExportDatalist and calculate Pagecount difference
			for (int i=0; i < NewDatalist.Count; i++){
				bool bNewSN=false;
				if (NewDatalist[i].SN != Printerlist[i].SN) {bNewSN = true;}
				Exportlist.Add(new ExportData {
					DEP = Printerlist[i].DEP,
					SN = NewDatalist[i].SN,
					PageCounterMono = NewDatalist[i].PageCounterMono,
					PageCounterColor = NewDatalist[i].PageCounterColor,
					MonthlyPagesMono = NewDatalist[i].PageCounterMono - Printerlist[i].PageCounterMono,
					MonthlyPagesColor = NewDatalist[i].PageCounterColor - Printerlist[i].PageCounterColor,
					Connected = NewDatalist[i].Active,
					NewSN = bNewSN
				});
				Printerlist[i].PageCounterMono = NewDatalist[i].PageCounterMono;
				Printerlist[i].PageCounterColor = NewDatalist[i].PageCounterColor;
			}
			//Write infos into ExportDatalist and calculate Pagecount difference END

			if (bDebug){
				for (int i = 0; i < Exportlist.Count; i++) {
					Console.WriteLine (Exportlist [i].DEP + " "
					+ Exportlist [i].SN + " "
					+ Exportlist [i].PageCounterMono + " "
					+ Exportlist [i].PageCounterColor + " "
					+ Exportlist [i].MonthlyPagesMono + " "
					+ Exportlist [i].MonthlyPagesColor + " "
					+ Exportlist [i].Connected + " "
					+ Exportlist [i].NewSN
					);
				}
				System.Console.ReadLine ();
			}
			if (bDBconnect){
				//TODO: Daten in die DB schreiben
			}


		}
		public static NewInfos QueryPrinter(string sIP, bool bDebug)
		{
			//initialize variables
			string sURL = "127.0.0.1";
			string sSN = "";
			string sHTML, sMatchPattern;
			int iPageCounterMono = 0;
			int iPageCounterColor = 0;
			Regex regEx;
			Match regMatch;
			//initialize variables END

			//accept all certificates
			ServicePointManager.ServerCertificateValidationCallback = delegate {return true;};

			//create URL string
			switch (getWebType(sIP)){
			case 1:
				sURL = "https://" + sIP + "/hp/device/InternalPages/Index?id=UsagePage";
				break;

			default:
				if(bDebug){
					System.Console.WriteLine("No WebType for printer " + sIP);
				}
				//TODO: Log schreiben
				return new NewInfos {SN = "NoWebType", PageCounterMono = 0, PageCounterColor = 0, Active = false};
			}
			//create URL string END

			//read source code from website
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create (sURL);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse ();
			StreamReader sr = new StreamReader (response.GetResponseStream ());
			sHTML = sr.ReadToEnd ();
			sr.Close ();
			response.Close ();
			//read source code from website END

			//getting informations with the webtype
			switch (getWebType(sIP)){
			case 1:
				sMatchPattern = "id=\"UsagePage[.]DeviceInformation[.]DeviceSerialNumber\">(..........)<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				sSN = regMatch.Groups [1].Value;

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Print[.]Monochrome\" class=\"align-right\">(\\d+).\\d+<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				iPageCounterMono = Convert.ToInt32 (regMatch.Groups [1].Value);

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Print[.]Color\" class=\"align-right\">(\\d+).\\d+<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				iPageCounterColor = Convert.ToInt32 (regMatch.Groups [1].Value);

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Copy[.]Monochrome\" class=\"align-right\">(\\d+).\\d+<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				iPageCounterMono += Convert.ToInt32 (regMatch.Groups [1].Value);

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Copy[.]Color\" class=\"align-right\">(\\d+).\\d+<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				iPageCounterColor += Convert.ToInt32 (regMatch.Groups [1].Value);

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Fax[.]Monochrome\" class=\"align-right\">(\\d+).\\d+<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				iPageCounterMono += Convert.ToInt32 (regMatch.Groups [1].Value);

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Fax[.]Color\" class=\"align-right\">(\\d+).\\d+<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				iPageCounterColor += Convert.ToInt32 (regMatch.Groups [1].Value);

				break;

			}
			return new NewInfos {SN = sSN, PageCounterMono = iPageCounterMono, PageCounterColor = iPageCounterColor, Active = true};
			//getting informations with the webtype END
		}

		public static int getWebType(string sIP)
		{
			//determines the WebType by checking the modelname
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create ("https://" + sIP);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse ();
			StreamReader sr = new StreamReader (response.GetResponseStream ());
			string sHTML = sr.ReadToEnd ();
			sr.Close ();
			response.Close ();
			string sMatchPattern = "<strong class=\"product\">(.+)<\\/strong>";
			Regex regEx = new Regex (sMatchPattern);
			Match regMatch = regEx.Match (sHTML);
			switch (regMatch.Groups[1].Value){
			//Cases for Webtype 1
			case "HP Officejet Color FlowMFP X585":
				return 1;

			default:
				return 0;
			}
		}
	}
}
