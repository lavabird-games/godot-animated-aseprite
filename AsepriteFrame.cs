using System;

using Godot;

namespace Lavabird.Plugins.AnimatedAseprite;

/// <summary>
/// Resource that defines a frame within an AsepriteAnimation.
/// </summary>
[Tool]
public class AsepriteFrame : Resource
{
	/// <summary>
	/// The region within the sprite sheet that contains the image we should render.
	/// </summary>
	[Export]
	public Rect2 Region { get; set; }

	/// <summary>
	/// The offset used to position the frame from the rendered Region.
	/// </summary>
	[Export]
	public Vector2 Offset { get; set; }

	/// <summary>
	/// The duration (in seconds) this frame should be shown for.
	/// </summary>
	[Export]
	public float Duration { get; set; }
}