using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Lavabird.Plugins.AnimatedAseprite.Importer;

/// <summary>
/// Strongly typed class representing the Aseprite exported JSON format.
/// </summary>
internal class AseJsonData
{
	#pragma warning disable CS8618 // Populated via reflection. We use Required attributes for null validation.

	// We are parsing this with our cusom resolver that sets the default required state of any field as
	// Required.Always. Any field that is be optional can then have an attribute setting it back to Required.Default.
	// This keeps the decorations to a minimum and makes this less horrible to read.
	
	// Frame data can be a hash or an array so we need a custom conversion
	[JsonConverter(typeof(AsjeJsonFrameDataConverter))]
	public List<FrameInfo> Frames { get; set; }

	public MetaHeader Meta { get; set; }

	public class FrameInfo
	{
		// This can be null, as the name might be in the key if hash instead of array
		[JsonProperty(Required = Required.Default)]
		public string? Filename { get; set; }

		public Rect Frame { get; set; }

		public bool Rotated { get; set; }

		public bool Trimmed { get; set; }

		public Rect SpriteSourceSize { get; set; }

		public Size SourceSize { get; set; }

		public int Duration { get; set; }
	}

	public class MetaHeader
	{
		public string App { get; set; }

		public string Version { get; set; }

		public string Image { get; set; }

		[JsonProperty(Required = Required.Default)]
		public string? Format { get; set; }

		public Size Size { get; set; }

		[JsonProperty(Required = Required.Default)]
		public float Scale { get; set; } = 1;

		// We can have no named animations defined (then we use the whole thing as one animation)
		[JsonProperty(Required = Required.Default)]
		public List<FrameTagInfo> FrameTags { get; set; }
	}

	public class FrameTagInfo
	{
		public string Name { get; set; }

		public int From { get; set; }

		public int To { get; set; }

		public string Direction { get; set; }
	}

	#pragma warning restore CS8618

	public class Size
	{
		public int w { get; set; }

		public int h { get; set; }
	}

	public class Rect : Size
	{
		public int x { get; set; }

		public int y { get; set; }
	}
}
