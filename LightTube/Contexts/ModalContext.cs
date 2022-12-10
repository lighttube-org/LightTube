namespace LightTube.Contexts;

public class ModalContext : BaseContext
{
	public string Title { get; set; }
	public ModalButton[] Buttons { get; set; }

	public ModalContext(HttpContext context) : base(context)
	{
	}
}

public class ModalButton
{
	public string Type;
	public string Target;
	public string Label;
	
	public ModalButton(string label, string target, string type)
	{
		Label = label;
		Target = target;
		Type = type;
	}
}