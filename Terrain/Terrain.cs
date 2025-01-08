using Godot;
using System;
using System.Linq;

// Generates from collision shapes and terrain border from polygons in editor
public class Terrain : StaticBody2D
{
	const float borderWidth = 30;
	
	Godot.Collections.Array editorPolygons;
	
	public override void _Ready()
	{

		editorPolygons = GetNode("Editor").GetChildren(); // All polygons are children of the same node

		// Generate collision shapes and border
		foreach (Polygon2D polygon in editorPolygons)
		{
			// Generate collision shapes
			CollisionPolygon2D newPolygon = new CollisionPolygon2D();
			newPolygon.Polygon = polygon.Polygon;
			newPolygon.Position = polygon.Position;
			AddChild(newPolygon);

			Line2D newBorder = new Line2D();

			// Use points of polygon as a starting point for border
			Vector2[] borderPoints = new Vector2[polygon.Polygon.Length + 2];
			polygon.Polygon.CopyTo(borderPoints, 0);

			// In order to prevent a single harsh corner where the line starts and ends, the start of the line is shifted to the middle of the edge. This makes the line seamless.
			borderPoints[0] = (polygon.Polygon[0] + polygon.Polygon[1]) / 2;
			borderPoints[borderPoints.Length - 2] = polygon.Polygon[0];
			

			// Shrink outline as to align the outer border with the outer edge of collision polygons
			for (int idx = 0; idx <= polygon.Polygon.Length; idx++)
			{
				
				Vector2 previousPoint = borderPoints[idx > 0 ? idx - 1 : borderPoints.Length - 2];
				Vector2 currentPoint = borderPoints[idx];
				Vector2 nextPoint = borderPoints[idx + 1];

				Vector2 line1 = currentPoint - previousPoint;
				Vector2 line2 = nextPoint - currentPoint;

				// Get angle between lines to find angle of corner and divide by two for angle of resulting offset vector
				float angleDifference = line1.AngleTo(line2);
				float startingHozAngle = line2.Angle();
				float offsetAngle = startingHozAngle + (Mathf.Pi - angleDifference) / 2; // Get half angle of corner
				
				// Generate vector from offset angle and set the magnitude to half the border width
				Vector2 offset = new Vector2(Mathf.Cos(offsetAngle), Mathf.Sin(offsetAngle)) * borderWidth / 2;

				// To decide whether this is inside or outside corner, check if point would be in polygon.
				if (Geometry.IsPointInPolygon(borderPoints[idx] + offset, polygon.Polygon))
				{
					borderPoints[idx] += offset;
				}
				else
				{
					borderPoints[idx] -= offset;
				}
			}

			// Complete the border line by repeating the first point of the border at the end
			borderPoints[borderPoints.Length - 1] = borderPoints[0];
			
			newBorder.Points = borderPoints;

			// Configure aesthetics of border
			newBorder.Width = borderWidth;
			newBorder.JointMode = Line2D.LineJointMode.Round;
			newBorder.EndCapMode = Line2D.LineCapMode.None;
			newBorder.BeginCapMode = Line2D.LineCapMode.Round;
			newBorder.DefaultColor = Colors.White;
			newBorder.Texture = GD.Load<StreamTexture>("res://Terrain/OutlineTexture.png");
			newBorder.TextureMode = Line2D.LineTextureMode.Tile;

			polygon.AddChild(newBorder);
		}
	}
}
