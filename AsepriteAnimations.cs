using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

namespace Lavabird.Plugins.AnimatedAseprite;

/// <summary>
/// Collection of Animation objects that define all the animations from an Aseprite export.
/// </summary>
[Tool]
public partial class AsepriteAnimations : Resource
{
	/// <summary>
	/// Gets the list of all animations in this AnimationData object.
	/// </summary>
	public IEnumerable<AsepriteAnimation> Animations { get => AnimationMap.Values; }

	/// <summary>
	/// Gets the names of all the animations in this AnimationData object.
	/// </summary>
	public IEnumerable<StringName> AnimationNames { get => AnimationMap.Keys; }

	/// <summary>
	/// Map of all animations we have stored keyed by animation name.
	/// </summary>
	[Export]
	private Godot.Collections.Dictionary<StringName, AsepriteAnimation> AnimationMap = new();

	/// <summary>
	/// Adds a new animation to the FrameData object.
	/// </summary>
	public void AddAnimation(StringName animationName, AsepriteAnimation animation)
	{
		AnimationMap.Add(animationName, animation);
	}

	/// <summary>
	/// Checks if an animation with the given name exists in this collection.
	/// </summary>
	public bool HasAnimation(StringName animationName)
	{
		return AnimationMap.ContainsKey(animationName);
	}

	/// <summary>
	/// Returns the Animation with the given name.
	/// </summary>
	public AsepriteAnimation this[StringName animationName]
	{
		get => AnimationMap[animationName];
	}
}
