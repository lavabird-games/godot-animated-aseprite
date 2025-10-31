using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Lavabird.Plugins.AnimatedAseprite.Importer;

/// <summary>
/// Strongly typed class representing the Aseprite exported JSON format.
/// </summary>
internal class AseJsonData
{
	// We are parsing this with our custom resolver that sets the default required state of any field as
	// Required.Always. Any field that is optional can then have an attribute setting it back to Required.Default.
	// This keeps the decorations to a minimum and makes this less horrible to read.
	
	// Frame data can be a hash or an array so we need a custom conversion
	[JsonConverter(typeof(AseJsonFrameDataConverter))]
	public required List<FrameInfo> Frames { get; set; }

	public required MetaHeader Meta { get; set; }

	public partial class FrameInfo
	{
		// This can be null, as the name might be in the key if hash instead of array
		[JsonProperty(Required = Required.Default)]
		public string? Filename { get; set; }

		public required Rect Frame { get; set; }

		public required bool Rotated { get; set; }

		public required bool Trimmed { get; set; }

		public required Rect SpriteSourceSize { get; set; }

		public required Size SourceSize { get; set; }

		public required int Duration { get; set; }
	}

	public partial class MetaHeader
	{
		public required string App { get; set; }

		public required string Version { get; set; }

		public required string Image { get; set; }

		[JsonProperty(Required = Required.Default)]
		public string? Format { get; set; }

		public required Size Size { get; set; }

		[JsonProperty(Required = Required.Default)]
		public required float Scale { get; set; } = 1;

		// We can have no named animations defined (then we use the whole thing as one animation)
		[JsonProperty(Required = Required.Default)]
		public List<FrameTagInfo>? FrameTags { get; set; }
	}

	public partial class FrameTagInfo
	{
		public required string Name { get; set; }

		public required int From { get; set; }

		public required int To { get; set; }

		public required string Direction { get; set; }
	}

	public partial class Size
	{
		public int W { get; set; }

		public int H { get; set; }
	}

	public partial class Rect : Size
	{
		public int X { get; set; }

		public int Y { get; set; }
	}
}
