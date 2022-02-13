using System.Collections.Generic;

namespace LightTube.Contexts
{
	public class LoginsContext : BaseContext
	{
		public List<LTLogin> Logins { get; set; }
	}
}