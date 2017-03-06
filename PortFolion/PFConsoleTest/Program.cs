using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortFolion.Core;

namespace PFConsoleTest {
	public class Program {
		static void Main(string[] args) {
			//var r = RootCollection.GetOrCreate(new DateTime(2017, 1, 1));
			//r.Name = "Broker01";
			//r.AddChild(new AccountNode(AccountClass.General) { Name = "Account01" });

			//var rr = RootCollection.GetOrCreate(new DateTime(2017, 2, 1));
			var ins = RootCollection.Instance;

			var b = new BrokerNode();
			b.Name = "Broker01";
			Console.WriteLine(b.Name);
			var n = new AccountNode(AccountClass.General);
			b.AddChild(n);
			var r = RootCollection.GetOrCreate(new DateTime(2017, 1, 1));
			r.Name = "総リスクファンド";
			r.AddChild(b);
			var rr = RootCollection.GetOrCreate(new DateTime(2017, 2, 1));
			Console.WriteLine(rr.Name);
			Console.ReadLine();
		}
	}
}
