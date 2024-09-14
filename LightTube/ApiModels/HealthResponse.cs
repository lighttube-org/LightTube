using InnerTube.Models;

namespace LightTube.ApiModels;

public class HealthResponse
{
	public int VideoHealth { get; set; }
	public double AveragePlayerResponseTime { get; set; }
	public CacheStats CacheStats { get; set; }
}