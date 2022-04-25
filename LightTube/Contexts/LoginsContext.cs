using System.Collections.Generic;
using LightTube.Database;

namespace LightTube.Contexts
{
	public class LoginsContext : BaseContext
	{
		public List<LTLogin> Logins { get; set; }
		public string CurrentLogin { get; set; }
	}
}