using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace NinjaSchool.Tilemaps
{
	[CreateAssetMenu(fileName = "New Water", menuName = "Tilemap/Water", order = 1)]
	public sealed class Water : Tile
	{
		[field: SerializeField]
		public bool isSurface { get; private set; }

		[ShowIf("isSurface")]
		[SerializeField]
		private Sprite[] animatedSprites;
		[ShowIf("isSurface")]
		[SerializeField]
		private float animationSpeed = 1, animationStartTime;


		public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
		{
			if (animatedSprites.Length == 0) return false;
			tileAnimationData.animatedSprites = animatedSprites;
			tileAnimationData.animationSpeed = animationSpeed;
			tileAnimationData.animationStartTime = animationStartTime;
			return true;
		}
	}
}
