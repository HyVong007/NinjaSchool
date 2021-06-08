using System;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace NinjaSchool.Tilemaps
{
	[DefaultExecutionOrder(-1)] // test
	public sealed class Tilemap : MonoBehaviour
	{
		[SerializeField] private UnityEngine.Tilemaps.Tilemap groundMap, waterMap;


		private static Tilemap instance;
		private void Awake()
		{
			instance = instance ? throw new Exception() : this;
			groundMap.CompressBounds();
			waterMap.CompressBounds();
			size = (waterMap.size = groundMap.size).ToVector2Int();
			waterMap.origin = groundMap.origin;
#if DEBUG
			if (groundMap.origin != Vector3Int.zero) throw new IndexOutOfRangeException();
#endif
			Box.Init(groundMap.size.y);
		}


		public static Vector2Int size { get; private set; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Water GetWaterTile(in Vector3Int index) => instance.waterMap.GetTile<Water>(index);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Tile GetGroundTile(in Vector3Int index) => instance.groundMap.GetTile<Tile>(index);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetGroundTile(in Vector3Int index, Tile tile) => instance.groundMap.SetTile(index, tile);


		/// <summary>
		/// Nếu <paramref name="collider"/> đi xuống va chạm với <see cref="Box"/> đang nổi trên nước thì gắn <paramref name="collider"/>.transform.parent vô box phát hiện va chạm.
		/// </summary>
		public static Vector3 FindMoveDestination(ICollider collider, Vector3 velocity)
		{
			var size = collider.size;
#if DEBUG
			if (size.x <= 0 || size.y <= 0) throw new ArgumentOutOfRangeException("size");
#endif
			Vector2 MIN = collider.transform.position;
			MIN.x -= 0.5f * size.x;
			var MAX = MIN + size;
			return new Vector3((velocity.x != 0 ? FindX() : MIN.x) + 0.5f * size.x, velocity.y != 0 ? FindY() : MIN.y);


			float FindX()
			{
				float xMin = MIN.x + velocity.x, xMax = MAX.x + velocity.x;
				var index = new Vector3Int(-1, (int)MIN.y, 0);
				int stopY = Mathf.CeilToInt(MAX.y) - 1;
				Tile tile;

				if (velocity.x < 0)
				{
					if ((index.x = (int)xMin) == xMin || 1 - xMin + index.x > 0.5f * size.x) goto NO_COLLISION;
					for (; index.y <= stopY; ++index.y)
						if ((tile = instance.groundMap.GetTile<Tile>(index)) && ((tile.collision & Tile.Edge.Right) == Tile.Edge.Right))
							return index.x + 1;
				}
				else if (velocity.x > 0)
				{
					if ((index.x = (int)xMax) == xMax || xMax - index.x > 0.5f * size.x) goto NO_COLLISION;
					for (; index.y <= stopY; ++index.y)
						if ((tile = instance.groundMap.GetTile<Tile>(index)) && ((tile.collision & Tile.Edge.Left) == Tile.Edge.Left))
							return index.x - size.x;
				}
				else throw new ArgumentOutOfRangeException("velocity");

				NO_COLLISION:
				return xMin;
			}


			float FindY()
			{
				float yMin = MIN.y + velocity.y, yMax = MAX.y + velocity.y;
				var index = new Vector3Int((int)MIN.x, -1, 0);
				int stopX = Mathf.CeilToInt(MAX.x) - 1;
				Tile tile;

				if (velocity.y < 0)
				{
					if ((index.y = (int)yMin) == yMin || 1 - yMin + index.y > 0.6f * size.y) return yMin;
					for (; index.x <= stopX; ++index.x)
						if ((tile = instance.groundMap.GetTile<Tile>(index)) && ((tile.collision & Tile.Edge.Top) == Tile.Edge.Top))
							return index.y + 1;
				}
				else if (velocity.y > 0)
				{
					if ((index.y = (int)yMax) == yMax || yMax - index.y > 0.5f * size.y) return yMin;
					for (; index.x <= stopX; ++index.x)
						if ((tile = instance.groundMap.GetTile<Tile>(index)) && ((tile.collision & Tile.Edge.Bottom) == Tile.Edge.Bottom))
							return index.y - size.y;
				}
				else throw new ArgumentOutOfRangeException("velocity");

				#region Check Box Collision
				if (Box.boxCount == 0) return yMin;
				var boxs = Box.boxs[index.y];
				if (boxs == null || boxs.Count == 0) return yMin;
				bool up = velocity.y > 0;
				float a = MIN.x - 0.5f, b = MAX.x + 0.5f;
				foreach (var box in boxs)
				{
					if (box.isFloating && (up || collider is Box)) continue;
					float x = box.transform.position.x;
					if (a <= x && x <= b)
					{
						if (box.isFloating) collider.transform.parent = box.transform;
						return index.y + (up ? -size.y : 1);
					}
				}
				#endregion

				return yMin;
			}
		}


		public static bool IsStandOnWaterSurface(ICollider collider)
		{
			var pos = collider.transform.position;
			int y = (int)pos.y;
			if (y == pos.y) return false;

			float dy = pos.y - y;
			if (dy < 0.4f || dy > 0.6f) return false;

			float sizeX = collider.size.x;
			pos.x -= 0.5f * sizeX;
			var index = new Vector3Int((int)pos.x, y, 0);
			for (int stopX = Mathf.CeilToInt(pos.x + sizeX) - 1; index.x <= stopX; ++index.x)
			{
				var water = instance.waterMap.GetTile<Water>(index);
				if (water && water.isSurface) return true;
			}

			return false;
		}
	}
}
