using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lavabird.Plugins.AnimatedAseprite.Importer;

/// <summary>
/// Custom JsonConverter to handle parsing FrameData definitions within an AseJsonData file.
/// </summary>
internal class AseJsonFrameDataConverter : JsonConverter<List<AseJsonData.FrameInfo>>
{
	/// <inheritdoc/>
	public override List<AseJsonData.FrameInfo>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// The file format has 2 valid structures. One where the frames are in a JSON array (recommended),
		// and a format where frames are an object with each frame having a key by name instead.
		
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}
		
		var frames = new List<AseJsonData.FrameInfo>();
		
		// Array of frames
		if (reader.TokenType == JsonTokenType.StartArray)
		{
			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndArray) break;

				var frameInfo = JsonSerializer.Deserialize<AseJsonData.FrameInfo>(ref reader, options);
				if (frameInfo != null)
				{
					frames.Add(frameInfo);
				}
			}
		}
		// Frames as subobjects (so each frame is a key)
		else if (reader.TokenType == JsonTokenType.StartObject)
		{
			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject) break;

				if (reader.TokenType == JsonTokenType.PropertyName)
				{
					// Skip the property name (frame name key)
					reader.Read();

					var frameInfo = JsonSerializer.Deserialize<AseJsonData.FrameInfo>(ref reader, options);
					if (frameInfo != null)
					{
						frames.Add(frameInfo);
					}
				}
			}
		}
		else
		{
			throw new JsonException($"Unexpected token type: {reader.TokenType}");
		}

		// Treat empty frames as null, since we're a required property anyway. This gives a better error message
		return frames.Count > 0 ? frames : null;
	}

	/// <inheritdoc/>
	public override void Write(Utf8JsonWriter writer, List<AseJsonData.FrameInfo> value, JsonSerializerOptions options)
	{
		// We don't need to write
		throw new NotImplementedException();
	}
}

