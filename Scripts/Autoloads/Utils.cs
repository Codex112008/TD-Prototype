using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using static Godot.Image;

public partial class Utils : Node
{
	public static Utils instance;
	public override void _EnterTree()
	{
		if (instance != null)
		{
			GD.PrintErr("More than one Utils in scene!");
			return;
		}
		instance = this;
	}

	public static void RemoveDirRecursive(string directory)
	{
		DirAccess dirAccess = DirAccess.Open(directory);
		foreach (string dir in dirAccess.GetDirectories())
			RemoveDirRecursive(directory.PathJoin(dir));
		foreach (string file in dirAccess.GetFiles())
			dirAccess.Remove(directory.PathJoin(file));
		dirAccess.Remove(directory);
	}

	// TODO: maybe move this to some util class // Update: yeah i moved it yay
	[GeneratedRegex("(?<!^)([A-Z])")]
	private static partial Regex PascalSplitRegex();
	private static readonly Regex sPascalCase = PascalSplitRegex();
	public static string SplitIntoPascalCase(string input)
	{
		// Inserts a space before each uppercase letter that is not the first character.
		// The pattern ensures that a space is inserted only if the uppercase letter
		// is preceded by a lowercase letter or another uppercase letter that is
		// part of an acronym (e.g., "GPSData" becomes "GPS Data").
		// IDFK how this works
		return sPascalCase.Replace(input, " $1").Trim();
	}

	public static string RemoveWhitespaces(string input)
	{
		return input.Replace(" ", "");
	}

	// Credits to random guy from the internet that had GDScript that I converted to C#
	public static Image CreateImageFromSprites(Tower towerToSave, Color towerColor = default)
	{
		Array<Sprite2D> sprites = towerToSave.SpritesForIcon;
		if (sprites.Count > 1)
		{
			Format format = sprites[0].Texture.GetImage().GetFormat();
			Rect2 boundingBox = GetSpriteRect(sprites[0], towerToSave);
			for (int i = 1; i < sprites.Count; i++)
				boundingBox = boundingBox.Merge(GetSpriteRect(sprites[i], towerToSave));

			if (sprites.All(sprite => sprite.Texture.GetImage().GetFormat() == format) && boundingBox.Size.X > 0 && boundingBox.Size.Y > 0)
			{
				Image image = CreateEmpty((int)boundingBox.Size.X, (int)boundingBox.Size.Y, false, format);

				foreach (Sprite2D sprite in sprites)
				{
					Image spriteImage = sprite.Texture.GetImage();
					if (sprite.SelfModulate != Colors.White && towerColor == default)
						spriteImage = ColorImage(spriteImage, sprite.SelfModulate);
					else if (towerColor != default)
						spriteImage = ColorImage(spriteImage, towerColor);

					image.BlendRect(spriteImage, new Rect2I(Vector2I.Zero, spriteImage.GetSize()), (Vector2I)(GetSpriteRect(sprite, towerToSave).Position - boundingBox.Position));
				}

				return image;
			}
			else
				return null;
		}
		else if (sprites.Count == 1)
			return sprites[0].Texture.GetImage();
		else
			return null;
	}

	private static Image ColorImage(Image image, Color color)
	{
		for (int i = 0; i < image.GetWidth(); i++)
		{
			for (int j = 0; j < image.GetHeight(); j++)
			{
				image.SetPixel(i, j, image.GetPixel(i, j) * color);
			}
		}

		return image;
	}

	public static Rect2 GetSpriteRect(Sprite2D sprite, Tower towerToSave)
	{
		Rect2 rect = new(towerToSave.ToLocal(sprite.GlobalPosition) + sprite.Offset, sprite.GetRect().Size);

		if (sprite.Centered)
			rect.Position -= rect.Size / 2f;

		return rect;
	}

	public static string AddCorrectDirectoryToPath(string pathWithoutDirectory)
	{
		return OS.HasFeature("editor") ? "res://" + pathWithoutDirectory : "user://" + pathWithoutDirectory;
	}

	public static Tuple<string, int> TrimNumbersFromString(string inputString)
    {
		int lastNonDigitIndex = inputString.Length - 1;
        while (lastNonDigitIndex >= 0 && char.IsDigit(inputString[lastNonDigitIndex]))
			lastNonDigitIndex--;

		if (lastNonDigitIndex == inputString.Length - 1)
			return new Tuple<string, int>(inputString, -1);;
		
		return new Tuple<string, int>(inputString[..(lastNonDigitIndex + 1)], int.Parse(inputString[(lastNonDigitIndex + 1)..]));
    }
}
