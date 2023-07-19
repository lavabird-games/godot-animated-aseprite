using System;
using System.Linq;

using Godot;

namespace Lavabird.Plugins.AnimatedAseprite;

/// <summary>
/// 2D sprite class, similar to the in-built AnimatedSprite but designed to read animations exported
/// from Aseprite or TexturePacker. Unlike AnimatedSprite it also supports a non-constant frame rate
/// for each displayed frame.
/// </summary>
[Tool]
public class AnimatedAseprite : Node2D
{
	/// <summary>
	/// Emitted when the current animation has finished playing.
	/// </summary>
	[Signal]
	public delegate void AnimationFinished();

	/// <summary>
	/// Emitted when this animtion changes frame.
	/// </summary>
	[Signal]
	public delegate void FrameChanged();

	/// <summary>
	/// Pure C# event for when a frame has been drawn. This is useful if you need precise frame timing (e.g. for updating
	/// shader information), and using a signal would be too slow.
	/// </summary>
	public event Action? FrameDrawn;

	/// <summary>
	/// The texture with the frames needed for this node's animations.
	/// </summary>
	[Export]
	public Texture? SpriteSheet
	{
		get => spriteSheet;
		set
		{
			spriteSheet = value;
			Update();
		}
	}
	private Texture? spriteSheet;

	/// <summary>
	/// The resource containing the animation definition.
	/// </summary>
	[Export]
	public AsepriteAnimations? AnimationData 
	{
		get => animationData;
		set
		{
			animationData = value;
			// If we had no animation or an invalid animation set from before then
			// we can just pick the first valid one from the list to use
			if (animationData != null && animationData.Animations.Count() > 0)
			{
				if(Animation == null || !animationData.HasAnimation(Animation))
				{
					Animation = animationData.AnimationNames.First();
				}
				Update();
			}
			PropertyListChangedNotify();
		}
	}
	private AsepriteAnimations? animationData;

	/// <summary>
	/// The name of the animation currently being played.
	/// </summary>
	// Property is not an [Export] as we define it manually in _GetPropertyList.
	public string? Animation
	{
		get => animation;
		set
		{
			if(animation != value && value != null && AnimationData != null && AnimationData.HasAnimation(value))
			{
				// Reset to end frame for the chosen animation
				var anim = AnimationData[value];
				if(anim.Direction == AsepriteAnimation.AnimationDirection.Forward || 
					anim.Direction == AsepriteAnimation.AnimationDirection.PingPong)
				{
					Frame = 0;
				}
				else
				{
					Frame = anim.Frames.Count - 1;
				}
				// Changing frame will call Update so we don't need to
			}
			animation = value;
		}
	}
	private string? animation;

	/// <summary>
	/// The displayed animation frame's index.
	/// </summary>
	[Export]
	public int Frame 
	{
		get => frame;
		set
		{
			if (AnimationData != null && Animation != null && AnimationData.HasAnimation(Animation))
			{
				if (value < AnimationData[Animation].Frames.Count)
				{
					// We only notify if changed, but always reset time and redraw
					if (frame != value)
					{ 
						frame = value;

						// At higher framerates this makes it impossible to change inspector values whilst the sprite
						// is playing. Godot rebuilds the whole panel every time it's called which prevents edits
						// in the inspector unless they happen within a single frame. This means we can't replicate the 
						// same behaviour from AnimatedSprite where frame increases during playback. Instead we only
						// update when we are stopped.
						if (!Playing)
						{
							PropertyListChangedNotify();
						}
					}

					elapsed = 0;
					Update();
				}
				else
				{
					GD.PushError($"Frame index {value} is out of bounds for animation '{Animation}' ({AnimationData[Animation].Frames.Count} frames)");
				}
			}
		}
	}
	private int frame;

	/// <summary>
	/// The animation speed is multiplied by this value.
	/// </summary>
	[Export]
	public float SpeedScale 
	{
		get => speedScale;
		set
		{
			speedScale = value > 0 ? value : 0;
		}
	}
	private float speedScale = 1f;

	/// <summary>
	/// Gets whether the animation is currently playing.
	/// </summary>
	[Export]
	public bool Playing
	{
		get => playing;
		set
		{
			playing = value;
			// Don't need to update if not playing anymore
			SetProcess(playing);
		}
	}
	private bool playing = false;

	/// <summary>
	/// Whether the animation being played should loop (will still trigger signals at end of
	/// each loop iteration as the animation is finished).
	/// </summary>
	[Export]
	public bool Loop { get; set; } = false;

	/// <summary>
	/// If true, frames will be centered.
	/// </summary>
	[Export]
	public bool Centered 
	{
		get => centered;
		set
		{
			centered = value;
			Update();
		}
	}
	private bool centered = true;

	/// <summary>
	/// The texture's drawing offset.
	/// </summary>
	[Export]
	public Vector2 Offset
	{
		get => offset;
		set
		{
			offset = value;
			Update();
		}
	}
	private Vector2 offset;

	/// <summary>
	/// If true, sprite textures and offsets are flipped horizontally.
	/// </summary>
	[Export]
	public bool FlipH
	{
		get => flipH;
		set
		{
			flipH = value;
			Update();
		}
	}
	private bool flipH = false;

	/// <summary>
	/// If true, sprite textures and offsets are flipped horizontally.
	/// </summary>
	[Export]
	public bool FlipV
	{
		get => flipV;
		set
		{
			flipV = value;
			Update();
		}
	}
	private bool flipV = false;

	/// <summary>
	/// Amount of time elapsed (in seconds) since we last changed frame.
	/// </summary>
	private float elapsed;

	/// <summary>
	/// Whether we are currently playing forwards or backwards. This is an internal state for the current play,
	/// not the state from the animation (i.e. this will alternate if the animation has a direction of PingPong).
	/// </summary>
	private bool forward = true;

	/// <inheritdoc/>
	public override void _Ready()
	{
		Update();
	}

	/// <inheritdoc/>
	public override void _Process(float delta)
	{
		elapsed += (delta * SpeedScale);

		if (AnimationData != null && Animation != null && AnimationData.HasAnimation(Animation))
		{
			var animation = AnimationData[Animation];

			// If we lagged, we might need to skip multiple frames to catch up
			while(Playing && animation.Frames[Frame].Duration < elapsed)
			{
				elapsed -= animation.Frames[Frame].Duration;

				if(forward)
				{
					// Go to the next frame if we have one, else we have finished the animation
					if(Frame < animation.Frames.Count - 1)
					{
						Frame++;
						EmitSignal(nameof(FrameChanged));
					}
					else
					{
						OnAnimationFinished();
					}
				}
				else
				{
					if (Frame > 0)
					{
						Frame--;
						EmitSignal(nameof(FrameChanged));
					}
					else
					{
						OnAnimationFinished();
					}
				}

				Update();
			}
		}
	}

	/// <inheritdoc/>
	public override void _Draw()
	{
		if (AnimationData != null && Animation != null && AnimationData.HasAnimation(Animation))
		{
			var animation = AnimationData[Animation];

			// If we are out of range (we check this earlier, but the animation itself could be modified) then
			// we just render the first frame of the animation so we're not invisible.
			var frameIndex = frame < animation.Frames.Count ? frame : 0;
			var frameData = animation.Frames[frameIndex];

			// Calculate destination rect accomodating for flips and center
			var destOffset = new Vector2(
				FlipH ? (animation.FrameSize.x - frameData.Region.Size.x) - frameData.Offset.x : frameData.Offset.x,
				FlipV ? (animation.FrameSize.y - frameData.Region.Size.y) - frameData.Offset.y : frameData.Offset.y);
			var destSize = new Vector2((FlipH ? -1 : 1), FlipV ? -1 : 1) * frameData.Region.Size;

			if (Centered)
			{
				destOffset -= (animation.FrameSize / 2f);
			}

			// Render the texture from the sheet
			DrawTextureRectRegion(SpriteSheet, new Rect2(destOffset, destSize), frameData.Region);

			// Using signals was too slow for this (there was a 1 frame lag when updating)
			FrameDrawn?.Invoke();
		}
	}

	/// <summary>
	/// Plays the given animation. If no animation is given will play the current animation. 
	/// Frame will be reset to 0 if changing animation, otherwise will remain unchanged.
	/// </summary>
	public void Play(string? animation = null)
	{
		if(animation != null && AnimationData != null && AnimationData.HasAnimation(animation))
		{
			Animation = animation;

			Playing = true;
			Update();
		}
		else
		{
			GD.PushError($"Could not find animation '{animation}' in AnimationData.");
		}
	}

	/// <summary>
	/// Handles when the currently playing animation has finished playing.
	/// </summary>
	private void OnAnimationFinished()
	{
		if (Loop)
		{
			// Its possible we could have been altered during playback, so need to recheck
			if (Animation != null && AnimationData != null && AnimationData.HasAnimation(Animation))
			{
				// Where the "start" of the next loop is will depend which direction we are playing
				switch (AnimationData[Animation].Direction)
				{
					case AsepriteAnimation.AnimationDirection.Forward:
						Frame = 0;
						break;
					case AsepriteAnimation.AnimationDirection.Reverse:
						Frame = Math.Max(0, AnimationData[Animation].Frames.Count - 1); // -1 for index
						break;
					case AsepriteAnimation.AnimationDirection.PingPong:
					case AsepriteAnimation.AnimationDirection.PingPongReverse:
						// For pingpong we will have just hit the duration of the last frame, and so need the next frame
						if (forward)
						{
							// Now reversing
							Frame = Math.Max(0, AnimationData[Animation].Frames.Count - 2); // -1 for index, -1 again for previous
						}
						else
						{
							// Now forward again
							Frame = AnimationData[Animation].Frames.Count > 0 ? 1 : 0;
						}
						forward = !forward;
						break;
				}
			}
		}
		else
		{
			Playing = false;
			PropertyListChangedNotify();
		}

		EmitSignal(nameof(AnimationFinished));
	}

	/// <summary>
	/// Gets the total number of frames for the currently active animation.
	/// </summary>
	public int TotalFrames 
	{ 
		get
		{
			if (AnimationData != null && Animation != null && AnimationData.HasAnimation(Animation))
			{
				return AnimationData[Animation].Frames.Count();
			}

			return 0;
		}
	}

	/// <inheritdoc/>
	public override Godot.Collections.Array _GetPropertyList()
	{
		var propertyList = new Godot.Collections.Array();

		// Include Animation property as a dropdown (we're still a freeform string property in code)
		// but this makes it nicer to work with in the editor.
		// In G4 we could categorise this so it doesn't appear at the bottom of the list
		// It should appear 3rd (after AnimationData instead)
		var propertyDef = new Godot.Collections.Dictionary<string, object>();
		propertyDef["name"] = "Animation";
		propertyDef["type"] = Variant.Type.String;
		propertyDef["hint"] = PropertyHint.Enum;
		propertyDef["hint_string"] = AnimationData != null ? String.Join(",", AnimationData.AnimationNames) : "default";

		propertyList.Add(propertyDef);

		return propertyList;
	}

	/// <summary>
	/// Displays configuration error message in the editor. This lets implementors know if they haven't
	/// properly configured the node at design time. This does not exclude more issues being found
	/// at runtime by additional error checks.
	/// </summary>
	public override string _GetConfigurationWarning()
	{
		var validate = ValidateDesignTimeConfiguration();
		if (!validate.Valid)
		{
			return validate.Error!;
		}

		return String.Empty;    // Godot wants empty for no message, not null
	}

	/// <summary>
	/// Validates the design time properties (the properties normally set in the editor).
	/// </summary>
	protected (bool Valid, string? Error) ValidateDesignTimeConfiguration()
	{
		if (SpriteSheet == null)
		{
			return (false, "SpriteSheet property must be set with a sprite sheet texture.");
		}

		if (AnimationData == null)
		{
			return (false, "AnimationData property must be set with an Aseprite animation definition.");
		}

		if (String.IsNullOrEmpty(Animation) || !AnimationData.HasAnimation(Animation!))
		{
			return (false, "Animation property must be set to a named animation from AnimationData.");
		}

		return (true, null);
	}
}
