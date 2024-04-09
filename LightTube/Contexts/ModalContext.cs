namespace LightTube.Contexts;

public class ModalContext(HttpContext context) : BaseContext(context)
{
	public string Title { get; set; }
	public ModalButton[] Buttons { get; set; }
	public bool AlignToStart { get; set; }
}

public class ModalButton(string label, string target, string type)
{
	public string Type = type;
	public string Target = target;
	public string Label = label;
}