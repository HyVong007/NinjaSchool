using Cysharp.Threading.Tasks;
using NinjaSchool.Tilemaps;
using System;
using UnityEngine;


namespace NinjaSchool.Items
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
	public abstract class Item : MonoBehaviour
	{
		public Vector2 velocity, gravity;

		[Tooltip("Ma sát")]
		[Range(0, 1)]
		public float friction = 0.01f;

		[Tooltip("Đàn hồi")]
		[Range(0, 1)]
		public float bounce = 1;


		public async UniTask Move()
		{
			while (gameObject.activeSelf)
			{
				velocity += gravity;
				velocity.y *= 1 - friction;
				velocity.y = Mathf.Abs(velocity.y) <= 0.49f ? velocity.y : 0.49f * (velocity.y >= 0 ? 1 : -1);

				var dest = (Vector2)transform.position + velocity;
				//if (!Tile.border.TrueContains(dest))
				//{
				//	// Hủy Item
				//	Destroy(gameObject);
				//	return;
				//}

				//if (Tile.CheckTopEdgeCollision(ref dest)) velocity.y *= -bounce;
				transform.position = dest;
				await UniTask.Yield();
			}
		}


		protected void OnTriggerEnter2D(Collider2D collision)
		{
			throw new NotImplementedException();
		}
	}
}