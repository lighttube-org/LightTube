using System.Text;
using System.Web;
using System.Xml;
using InnerTube;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers;

[Route("/opensearch")]
public class OpenSearchController : Controller
{
	private readonly InnerTube.InnerTube _youtube;

	public OpenSearchController(InnerTube.InnerTube youtube)
	{
		_youtube = youtube;
	}

	[Route("osdd.xml")]
	public IActionResult OpenSearchDescriptionDocument()
	{
		XmlDocument doc = new();
		
		XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
		doc.AppendChild(docNode);

		XmlElement root = doc.CreateElement("OpenSearchDescription");

		XmlElement shortName = doc.CreateElement("ShortName");
		shortName.InnerText = "LightTube";
		root.AppendChild(shortName);

		XmlElement description = doc.CreateElement("Description");
		description.InnerText = "Search for videos on LightTube";
		root.AppendChild(description);

		XmlElement inputEncoding = doc.CreateElement("InputEncoding");
		inputEncoding.InnerText = "UTF-8";
		root.AppendChild(inputEncoding);

		XmlElement image = doc.CreateElement("Image");
		image.SetAttribute("width", "16");
		image.SetAttribute("height", "16");
		image.SetAttribute("type", "image/vnd.microsoft.icon");
		image.InnerText = $"https://{Request.Host}/favicon.ico";
		root.AppendChild(image);

		XmlElement imageHq = doc.CreateElement("Image");
		imageHq.SetAttribute("width", "96");
		imageHq.SetAttribute("height", "96");
		imageHq.SetAttribute("type", "image/png");
		imageHq.InnerText = $"https://{Request.Host}/icons/favicon-96x96.png";
		root.AppendChild(imageHq);

		XmlElement searchUrl = doc.CreateElement("Url");
		searchUrl.SetAttribute("type", "text/html");
		searchUrl.SetAttribute("template", $"https://{Request.Host}/results?search_query={{searchTerms?}}");
		root.AppendChild(searchUrl);
		
		XmlElement suggestionsUrl = doc.CreateElement("Url");
		suggestionsUrl.SetAttribute("type", "application/x-suggestions+json");
		suggestionsUrl.SetAttribute("template", $"https://{Request.Host}/opensearch/suggestions.json?q={{searchTerms?}}");
		root.AppendChild(suggestionsUrl);
		
		doc.AppendChild(root);
		doc.DocumentElement?.SetAttribute("xmlns", "http://a9.com/-/spec/opensearch/1.1/");
		doc.DocumentElement?.SetAttribute("xmlns:moz", "http://www.mozilla.org/2006/browser/search/");

		return File(Encoding.UTF8.GetBytes(doc.OuterXml), "application/opensearchdescription+xml");
	}

	[Route("suggestions.json")]
	public async Task<object[]> Suggestions(string q)
	{
		object[] res = new object[4];
		res[0] = q;
		res[1] = new List<string>();
		res[2] = new List<string>();
		res[3] = new List<string>();
		InnerTubeSearchAutocomplete autocomplete = await _youtube.GetSearchAutocompleteAsync(q);
		foreach (string s in autocomplete.Autocomplete)
		{
			(res[1] as List<string>)!.Add(s);
			(res[2] as List<string>)!.Add("");
			(res[3] as List<string>)!.Add($"https://{Request.Host}/results?search_query={HttpUtility.UrlEncode(s)}");
		}

		return res;
	}
	
}