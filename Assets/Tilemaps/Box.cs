using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace NinjaSchool.Tilemaps
{
	public sealed class Box : MonoBehaviour, ICollider
	{
		public Vector2 size { get; } = new Vector2(1, 1);
		/// <summary>
		/// Đang nổi trên mặt nước ?
		/// </summary>
		[field: SerializeField]
		public bool isFloating { get; private set; }
		public static ReadOnlyArray<IReadOnlyList<Box>> boxs { get; private set; }
		public static int boxCount { get; private set; }


		/// <summary>
		/// Code nhanh: <c>(array[i]??=new List&lt;Box&gt;()).Method()</c> bởi vì array[i] có thể <see langword="null"/>
		/// </summary>
		private static List<Box>[] array;
		public static void Init(int mapHeight)
		{
			array = new List<Box>[mapHeight];
			boxs = new ReadOnlyArray<IReadOnlyList<Box>>(array);
			boxCount = 0;
		}


		private void Awake()
		{
			(array[(int)transform.position.y] ??= new List<Box>()).Add(this);
			++boxCount;
		}


		private void OnDisable()
		{
			array[(int)transform.position.y].Remove(this);
			--boxCount;
		}


		[SerializeField]
		[Expandable]
		private Tile tile;
		public async UniTask Move(float vx)
		{
			var v = new Vector3(vx, 0);
			var dest = Tilemap.FindMoveDestination(this, v);
			var pos = transform.position;
			if (dest == pos)
			{
				dest.x -= 0.5f;
				Tilemap.SetGroundTile(dest.ToVector3Int(), tile);
				Destroy(gameObject);
				return;
			}

			transform.position = dest;
			await UniTask.Delay(15);
			if (Tilemap.FindMoveDestination(this, fallVector).y < dest.y)
			{
				Fall().Forget();
				return;
			}

			if (Mathf.Abs(dest.x - pos.x) < Mathf.Abs(vx))
			{
				dest.x -= 0.5f;
				Tilemap.SetGroundTile(dest.ToVector3Int(), tile);
				Destroy(gameObject);
			}
		}


		private static readonly Vector3 fallVector = new Vector3(0, -0.19f);
		private async UniTask Fall()
		{
			array[(int)transform.position.y].Remove(this);
			while (true)
			{
				float y = (transform.position += fallVector).y;
				await UniTask.Delay(15);
				if (Tilemap.IsStandOnWaterSurface(this))
				{
					(array[(int)y] ??= new List<Box>()).Add(this);
					Floating();
					return;
				}

				var dest = Tilemap.FindMoveDestination(this, fallVector);
				if (dest.y >= y)
				{
					transform.position = dest;
					(array[(int)dest.y] ??= new List<Box>()).Add(this);
					var pos = Tilemap.FindMoveDestination(this, floatingMoveVector);
					if (pos.x == dest.x)
					{
						pos.x -= 0.5f;
						Tilemap.SetGroundTile(pos.ToVector3Int(), tile);
						Destroy(gameObject);
						return;
					}

					pos = Tilemap.FindMoveDestination(this, floatingMoveVector * -1);
					if (pos.x == dest.x)
					{
						pos.x -= 0.5f;
						Tilemap.SetGroundTile(pos.ToVector3Int(), tile);
						Destroy(gameObject);
					}

					return;
				}
			}
		}


		[SerializeField] private Animator animator;
		private static Vector3 floatingMoveVector = new Vector3(0.01f, 0);
		/// <summary>
		/// Nếu Box nằm ngoài camera thì pause !
		/// </summary>
		private async void Floating()
		{
			animator.enabled = isFloating = true;
			var pos = transform.position;
			int y = (int)pos.y;
			pos.y = y;
			transform.position = pos;
			int startX = (int)(pos.x - 0.5f), stopX = startX = Tilemap.GetWaterTile(new Vector3Int(startX, y, 0)) ?
				startX : Mathf.CeilToInt(pos.x + 0.5f) - 1;
			bool determined_StartX = false, determined_StopX = false;
			floatingMoveVector.x *= Random.Range(0, 2) == 1 ? 1 : -1;

			while (true)
			{
				pos += floatingMoveVector;
				if (floatingMoveVector.x > 0)
				{
					#region Tìm giới hạn PHẢI và kiểm tra pos.x
					if (!determined_StopX && !Tilemap.GetWaterTile(new Vector3Int(++stopX, y, 0)))
					{
						determined_StopX = true;
						--stopX;
					}

					if (determined_StopX && pos.x - 0.5f > stopX)
					{
						pos.x = stopX + 0.5f;
						floatingMoveVector.x *= -1;
					}
					#endregion
				}
				else
				{
					#region Tìm giới hạn TRÁI và kiểm tra pos.x
					if (!determined_StartX && !Tilemap.GetWaterTile(new Vector3Int(--startX, y, 0)))
					{
						determined_StartX = true;
						++startX;
					}

					if (determined_StartX && pos.x - 0.5f < startX)
					{
						pos.x = startX + 0.5f;
						floatingMoveVector.x *= -1;
					}
					#endregion
				}

				transform.position = pos;
				await UniTask.Delay(15);
			}
		}


		private void Start()
		{
			// Test
			Floating();
		}
	}
}