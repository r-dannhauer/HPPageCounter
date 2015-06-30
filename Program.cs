using System;
//using MySql.Data;
//using MySql.Data.MySqlClient;
//using MySql.Data.Types;
using System.Net.NetworkInformation;
using System.Net;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

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
			public int PageCounterMono { get; set; }
			public int PageCounterColor { get; set; }
		}
		public class NewInfos{
			public string SN { get; set; }
			public int PageCounterMono { get; set; }
			public int PageCounterColor { get; set; }
			public bool Active { get; set; }
			public int WebType { get; set; }
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

		[STAThread]
		public static void Main (string[] args)
		{
			
			//Creating variables

			bool bDebug = true;
			string sLine;
			string [] sPrinters;
			var Printerlist = new List<Printer> ();
			var NewDatalist = new List<NewInfos> ();
			var Exportlist = new List <ExportData> ();
			Ping pConnected = new Ping ();
			IPAddress ipConnected;
			StreamReader sr;
			StreamWriter sw;
			StreamWriter logfile;
			StreamWriter sw2;

			//Creating variables END


			//reading CSV file

			OpenFileDialog openFileDialog1 = new OpenFileDialog ();
			if (openFileDialog1.ShowDialog () == DialogResult.OK) {
				sr = new StreamReader (openFileDialog1.FileName);
				logfile = new StreamWriter (openFileDialog1.FileName + "_logfile.txt");
			} else {
				System.Console.WriteLine ("Program stopped because of a critical error: No file chosen!");
				System.Console.ReadLine ();
				return;
			}

			//reading CSV file END


			//reading printers

			System.Console.WriteLine ("Reading Printer list.");
			logfile.WriteLine ("Reading Printer list.");
			while ((sLine = sr.ReadLine()) != null){
				sPrinters = sLine.Split (';');
				Printerlist.Add (new Printer {
					DEP = sPrinters [0],
					SN = sPrinters [1],
					IP = sPrinters [2],
					Active = Convert.ToBoolean(sPrinters [3]),
					PageCounterMono = Convert.ToInt32(sPrinters [4]),
					PageCounterColor = Convert.ToInt32(sPrinters [5])
				});
			}
			logfile.WriteLine ("Read " + Printerlist.Count + " printers.");

			//reading printers END


			//Query printers

			for (int i=0; i < Printerlist.Count; i++)
			{
				
				//Check if IP is valid
				if (IPAddress.TryParse (Printerlist [i].IP, out ipConnected)) {
					if (bDebug) {
						System.Console.WriteLine ("Valid IP Adress.");
					}
				}else{
					if (bDebug){
						System.Console.WriteLine ("Invalid IP Adress: " + Printerlist [i].DEP + " " + Printerlist [i].IP);
					}
					logfile.WriteLine("WARNING: Invalid IP Adress: " + Printerlist [i].DEP + " " + Printerlist [i].IP);
					continue;
				}

				//Check if printer is online
				if (bDebug) {
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
						logfile.WriteLine ("WARNING: Active printer not reachable! " + Printerlist [i].DEP + " " + Printerlist [i].IP);
						NewDatalist.Add (new NewInfos {
							SN = Printerlist [i].SN,
							PageCounterMono = Printerlist [i].PageCounterMono,
							PageCounterColor = Printerlist [i].PageCounterColor,
							Active = false,
							WebType = 0
						});
					}else{
						NewDatalist.Add (new NewInfos {
							SN = Printerlist [i].SN,
							PageCounterMono = Printerlist [i].PageCounterMono,
							PageCounterColor = Printerlist [i].PageCounterColor,
							Active = false,
							WebType = 0
						});
					}
					continue;
				}

				//Quering printer and writing the results in a list
				NewDatalist.Add (QueryPrinter (Printerlist [i].IP, bDebug));
				if (NewDatalist [i].WebType == 0) {
					logfile.WriteLine ("WARNING: Printer " + Printerlist [i].DEP + " - " + Printerlist[i].IP + " has no WebType!");
				}
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
			}

			//Exporting into CSV

			SaveFileDialog saveFileDialog1 = new SaveFileDialog ();
			if (saveFileDialog1.ShowDialog () == DialogResult.OK) {
				sw = new StreamWriter (saveFileDialog1.FileName);
			} else {
				System.Console.WriteLine ("Program stopped because of a critical error: No file chosen!");
				System.Console.ReadLine ();
				return;
			}
			for (int i = 0; i < Exportlist.Count; i++) {
				//Export for new data
				sLine = (Exportlist [i].DEP + ";"
					+ Exportlist [i].SN + ";"
					+ Exportlist [i].PageCounterMono + ";"
					+ Exportlist [i].PageCounterColor + ";"
					+ Exportlist [i].MonthlyPagesMono + ";"
					+ Exportlist [i].MonthlyPagesColor + ";"
					+ Exportlist [i].Connected + ";"
					+ Exportlist [i].NewSN
				);
				sw.WriteLine (sLine);
				//Export for new Printerlist
				sLine = (Exportlist [i].DEP + ";"
					+ Exportlist [i].SN + ";"
					+ Printerlist [i].IP + ";"
					+ Exportlist [i].Connected + ";"
					+ Exportlist [i].PageCounterMono + ";"
					+ Exportlist [i].PageCounterColor
				);
				sw2.WriteLine (sLine);
			}
			sw.Close ();
			sw2.Close ();
			logfile.WriteLine ("Exported " + Exportlist.Count + " printers successfully.");
			Console.WriteLine ("Exported " + Exportlist.Count + " printers successfully.");

			//Exporting into CSV END

			logfile.Close ();
			Console.ReadLine ();
		}


		public static NewInfos QueryPrinter(string sIP, bool bDebug)
		{
			
			//initialize variables

			string sURL = "127.0.0.1";
			string sSN = "";
			string sHTML, sMatchPattern, sTemp;
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
			case 2:
				sURL = "https://" + sIP + "/hp/device/InternalPages/Index?id=UsagePage";
				break;

			default:
				if(bDebug){
					System.Console.WriteLine("No WebType for printer " + sIP);
				}
				return new NewInfos {SN = "NoWebType", PageCounterMono = 0, PageCounterColor = 0, Active = false, WebType = 0};
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

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Print[.]Monochrome\" class=\"align-right\">(\\d+)[.]\\d<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);

				iPageCounterMono = Convert.ToInt32 (regMatch.Groups [1].Value);

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Print[.]Color\" class=\"align-right\">(\\d+)[.]\\d<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				sTemp = regMatch.Groups[1].Value.Replace(",", "");
				iPageCounterColor = Convert.ToInt32 (sTemp);

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Copy[.]Monochrome\" class=\"align-right\">(\\d+)[.]\\d<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				sTemp = regMatch.Groups[1].Value.Replace(",", "");
				iPageCounterMono += Convert.ToInt32 (sTemp);

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Copy[.]Color\" class=\"align-right\">(\\d+)[.]\\d<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				sTemp = regMatch.Groups[1].Value.Replace(",", "");
				iPageCounterColor += Convert.ToInt32 (sTemp);

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Fax[.]Monochrome\" class=\"align-right\">(\\d+)[.]\\d<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				sTemp = regMatch.Groups[1].Value.Replace(",", "");
				iPageCounterMono += Convert.ToInt32 (sTemp);

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Fax[.]Color\" class=\"align-right\">(\\d+)[.]\\d<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				sTemp = regMatch.Groups[1].Value.Replace(",", "");
				iPageCounterColor += Convert.ToInt32 (sTemp);

				break;

			case 2:
				sMatchPattern = "id=\"UsagePage[.]DeviceInformation[.]DeviceSerialNumber\">(..........)<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				sSN = regMatch.Groups [1].Value;

				sMatchPattern = "id=\"UsagePage[.]EquivalentImpressionsTable[.]Print[.]Total\" class=\"align-right\">(\\d+)[.]\\d<";
				regEx = new Regex (sMatchPattern);
				regMatch = regEx.Match (sHTML);
				sTemp = regMatch.Groups [1].Value.Replace (",", "");
				iPageCounterMono = Convert.ToInt32 (sTemp);

				break;

			}
			return new NewInfos {SN = sSN, PageCounterMono = iPageCounterMono, PageCounterColor = iPageCounterColor, Active = true, WebType = getWebType(sIP)};

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

			//Cases for Webtype 2

			case "HP LaserJet 600 M602":
				return 2;

			default:
				return 0;
			}
		}
	}
}
