using System;
using System.Linq;

using Godot;

namespace Lavabird.Plugins.AnimatedAseprite;

/// <summary>
/// Resource that defines an Aseprite animation.
/// </summary>
[Tool]
public class AsepriteAnimation : Resource
{
	/// <summary>
	/// The different animation directions we support.
	/// </summary>
	public enum AnimationDirection { Forward, Reverse, PingPong, PingPongReverse };
	
	/// <summary>
	/// List of frames this animation has.
	/// </summary>
	[Export]
	public Godot.Collections.Array<AsepriteFrame> Frames { get; private set; }

	/// <summary>
	/// The size (in pixels) of the frames for this animation.
	/// </summary>
	[Export]
	public Vector2 FrameSize { get; set; }

	/// <summary>
	/// The direction (e.g. forwards, reverse) used by this animation.
	/// </summary>
	[Export]
	public AnimationDirection Direction { get; set; } = AnimationDirection.Forward;

	/// <summary>
	/// Animation duration in seconds.
	/// </summary>
	public float Duration { get => Frames.Sum(f => f.Duration); }

	public AsepriteAnimation()
	{
		Frames = new Godot.Collections.Array<AsepriteFrame>();
	}
}
