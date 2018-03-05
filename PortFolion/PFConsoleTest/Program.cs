using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortFolion.Core;
using System.Net;
using System.Net.Http;

namespace PFConsoleTest {
	public class Program {
	/*
	 * html解析
	 * HtmlAgilityPack
	 * AngleSharp
	 * */
		static string url = "https://www.sbisec.co.jp/ETGate/";
		static string id;
		static string userPass;

		static void Main(string[] args) {

			
			Console.ReadLine();
		}
		static async Task<CookieContainer> LoginAsync(){
			CookieContainer cc;
			using (var handler = new HttpClientHandler())
			using(var client = new HttpClient(handler)){
				var content = new FormUrlEncodedContent(new Dictionary<string, string> {
					{"user_id", id },
					{"user_password",userPass },
				});
				await client.PostAsync(url, content);
				cc = handler.CookieContainer;
			}
			CookieCollection cookies = cc.GetCookies(new Uri(url));
			return cc;
		}
	}
}
