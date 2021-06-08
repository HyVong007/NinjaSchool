using NaughtyAttributes;
using System;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace NinjaSchool.Tilemaps
{
	[CreateAssetMenu(fileName = "New Tile", menuName = "Tilemap/Tile", order = 0)]
	public class Tile : TileBase
	{
		[Flags]
		public enum Edge
		{
			Top = 1 << 0,
			Right = 1 << 1,
			Bottom = 1 << 2,
			Left = 1 << 3
		}
		[field: SerializeField]
		public Edge collision { get; private set; }
		[SerializeField]
		[ShowAssetPreview]
		protected Sprite sprite;
		[SerializeField] protected Color color = Color.white;


		public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
		{
			tileData.sprite = sprite;
			tileData.color = color;
		}
	}
}