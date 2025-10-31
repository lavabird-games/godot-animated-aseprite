#if TOOLS // For release builds, we only need the node definition, not the whole plugin

using System;
using System.Text.RegularExpressions;
using Godot;

using Lavabird.Plugins.AnimatedAseprite;
using Lavabird.Plugins.AnimatedAseprite.Importer;

// Plugin needs to be outside namespace

/// <summary>
/// Plugin that makes working with exported Aseprite animations easier.
/// </summary>
[Tool]
public partial class AnimatedAsepritePlugin : EditorPlugin, ISerializationListener
{
	#pragma warning disable CS8618 // We are using Godot's tree lifecycle not the ctor. Nothing instances us directly so is ok.                                                                                              

	/// <summary>
	/// The importer used to process 
	/// </summary>
	private AseJsonImportPlugin aseJsonImportPlugin;

	/// <summary>
	/// The file root this plugin is running from (normally res://addons/Lavabird.AnimatedAseprite)
	/// </summary>
	internal static string PluginRoot { get; private set; }

	#pragma warning restore CS8618

	/// <summary>
	/// Initialize the plugin. Docs suggest to use this rather than _Ready() for plugins.
	/// </summary>
	public override void _EnterTree()
	{
		PluginRoot = GetPluginFolderRoot();

		// Register plugin to parse JSON animation files
		aseJsonImportPlugin = new AseJsonImportPlugin();
		AddImportPlugin(aseJsonImportPlugin);

		// Register additional node type so it shows in editor
		RegisterCustomType("AnimatedAseprite", "Node2D", PluginRoot, "/AnimatedAseprite.cs", "/Assets/AnimatedAsepriteIcon.svg");
	}

	/// <summary>
	/// Used to cleanup the plugin in case we were disabled.
	/// </summary>
	public override void _ExitTree()
	{
		RemoveImportPlugin(aseJsonImportPlugin);
		
		RemoveCustomType("AnimatedAseprite");
	}

	/// <summary>
	/// Registers a custom node type used by the plugin.
	/// </summary>
	private void RegisterCustomType(string typeName, string godotBase, string pluginPath, string scriptPath, string iconPath)
	{
		var script = GD.Load<Script>(pluginPath + scriptPath);
		var icon = GD.Load<Texture2D>(pluginPath + iconPath);

		if (script != null && icon != null)
		{
			AddCustomType(typeName, godotBase, script, icon);
		}
		else
		{
			AnimatedAsepritePlugin.Error($"Unable to load required files for the {typeName} node type.");
		}
	}

	/// <summary>
	/// Gets the string to use as the base folder of the plugin (normally res://addons/Lavabird.AnimatedAseprite)
	/// </summary>
	private string GetPluginFolderRoot()
	{
		// There is no built-in method to get our plugin root, but we can grab this script as a resource and use its path
		var script = (CSharpScript)GetScript();
		var regex = new Regex("^(res:\\/\\/addons\\/[^\\/]+)\\/.*\\.cs");
		var match = regex.Match(script.ResourcePath);

		if (match.Success)
		{
			return match.Groups[1].Value;
		}

		AnimatedAsepritePlugin.Error("Unable to determine a base path for the plugin.");
		return string.Empty;
	}

	/// <summary>
	/// Helper message for any error messages the plugin produces.
	/// </summary>
	public static void Error(string message)
	{
		GD.PushError($"[AnimatedAseprite Plugin] {message}");
	}

	#region Hot-reload handling

	/// <inheritdoc/>
	public void OnBeforeSerialize()
	{
		
	}

	/// <inheritdoc/>
	public void OnAfterDeserialize()
	{
		// We lose the static PluginRoot when we are re-serialized, so we have to set it again
		AnimatedAsepritePlugin.PluginRoot = GetPluginFolderRoot();
	}

	#endregion
}

#endif
