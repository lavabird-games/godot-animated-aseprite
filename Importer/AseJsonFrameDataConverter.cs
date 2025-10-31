using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lavabird.Plugins.AnimatedAseprite.Importer;

/// <summary>
/// Custom JsonConverter to handle parsing FrameData definitions within an AseJsonData file.
/// </summary>
internal class AseJsonFrameDataConverter : JsonConverter
{
	/// <inheritdoc/>
	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		// The file format has 2 valid structures. One where the frames are in a JSON array (recommended),
		// and a format where frames are an object with each frame having a key by name instead.

		var token = JToken.Load(reader);

		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}

		if (token.Type == JTokenType.Array)
		{
			return token.ToObject<List<AseJsonData.FrameInfo>>();
		}
		else
		{
			// Not an array. We need to parse each frame manually and build the list
			var frames = new List<AseJsonData.FrameInfo>();

			foreach (var frameWrapper in token.Children())
			{
				// The wrapper is the key (frame name), and the first (only) child is the body with the frame info
				var frameData = frameWrapper.FirstOrDefault();
				var frameInfo = frameData?.ToObject<AseJsonData.FrameInfo>();
				if (frameInfo != null)
				{
					frames.Add(frameInfo);
				}
			}

			return frames;
		}
	}

	/// <inheritdoc/>
	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		// We don't need to write
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(List<AseJsonData.FrameInfo>);
	}
}

