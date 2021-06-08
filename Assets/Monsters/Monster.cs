using Cysharp.Threading.Tasks;
using NinjaSchool.Tilemaps;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;


namespace NinjaSchool.Monsters
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
	public abstract class Monster : MonoBehaviour
	{
		protected CancellationTokenSource cancelAllTask;


		protected void OnEnable()
		{
			cancelAllTask = new CancellationTokenSource();
		}


		protected void OnDisable()
		{
			cancelAllTask.Cancel();
			cancelAllTask.Dispose();
		}


		#region Die
		[Serializable]
		protected sealed class DeathData
		{
			public Vector2 velocity = new Vector2(0.02f, 0), gravity = new Vector2(0, -0.02f);

			[Tooltip("Ma sát")]
			[Range(0, 1)]
			public float friction = 0.01f;

			[Tooltip("Đàn hồi")]
			[Range(0, 1)]
			public float bounce = 0.8f;
		}
		[SerializeField] protected DeathData deathData;


		protected async UniTask Die()
		{
			// Xóa/ ẩn collider, body

			float lastY = float.MaxValue;
			while (gameObject.activeSelf)
			{
				deathData.velocity += deathData.gravity;
				deathData.velocity.y *= (1 - deathData.friction);
				deathData.velocity.y = Mathf.Abs(deathData.velocity.y) <= 0.49f ? deathData.velocity.y : 0.49f * (deathData.velocity.y >= 0 ? 1 : -1);

				var A = (Vector2)transform.position + deathData.velocity;
				//if (!Tile.border.TrueContains(A))
				//{
				//	// Hủy xác chết
				//	Destroy(gameObject);
				//	return;
				//}

				//var B = A;
				//if (Tile.CheckTopEdgeCollision(ref B) && B.y < lastY)
				//{
				//	lastY = B.y;
				//	deathData.velocity.y *= -deathData.bounce;
				//	transform.position = B;
				//}
				//else transform.position = A;
				await UniTask.Yield();
			}
		}
		#endregion


		protected bool ΔisRightFace = true;
		private static readonly Quaternion EULER_Y180 = Quaternion.Euler(0, 180, 0);
		protected virtual bool isRightFace
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ΔisRightFace;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => transform.rotation = (ΔisRightFace = value) ? Quaternion.identity : EULER_Y180;
		}


		protected Vector2 Δdirection;
		/// <summary>
		/// Di chuyển
		/// </summary>
		protected abstract Vector2 direction { get; set; }


		protected UniTask taskDamage = UniTask.CompletedTask;
		/// <summary>
		/// Nhớ gán <see cref="taskDamage"/>
		/// <para>Khóa input, đợi <see cref="taskAttack"/> sau đó hủy tất cả task</para>
		/// </summary>
		protected abstract UniTask Damage(bool right);


		protected UniTask taskAttack = UniTask.CompletedTask;
		/// <summary>
		/// Nhớ gán <see cref="taskAttack"/>
		/// </summary>
		protected abstract UniTask Attack();
	}
}