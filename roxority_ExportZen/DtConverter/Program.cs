using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DtConverter {
	class Program {
		static void Main (string [] args) {
			Console.WriteLine (DateTime.Now.Subtract (TimeSpan.FromDays (45)).Ticks);
			while (true)
				Console.WriteLine (new DateTime (long.Parse (Console.ReadLine ())));
		}
	}
}
