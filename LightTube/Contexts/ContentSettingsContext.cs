using InnerTube;

namespace LightTube.Contexts;

public class ContentSettingsContext : BaseContext
{
	public InnerTubeLocals Locals; 
	
	public ContentSettingsContext(HttpContext context, InnerTubeLocals locals) : base(context)
	{
		Locals = locals;
	}
}