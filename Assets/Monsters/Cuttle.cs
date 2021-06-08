using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;


namespace NinjaSchool.Monsters
{
	public sealed class Cuttle : Monster
	{
		[SerializeField] private SpriteRenderer spriteRenderer;
		[SerializeField] private Animator animator;
		[SerializeField] private Sprite jump, fall;
		[SerializeField] private float height, upSpeed, downSpeed;
		private float lowY, highY;


		private void Awake()
		{
			lowY = transform.position.y;
			highY = lowY + height;
		}


		protected override Vector2 direction { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

		protected override bool isRightFace
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}


		protected override async UniTask Attack()
		{
			cancelAllTask.Cancel();
			cancelAllTask.Dispose();
			cancelAllTask = new CancellationTokenSource();
			animator.enabled = false;
			spriteRenderer.sprite = fall;

			throw new System.NotImplementedException();

			// Nếu chưa hủy
			MoveToY(lowY).Forget();
		}


		protected override async UniTask Damage(bool right)
		{
			if (taskAttack.Status == UniTaskStatus.Pending)
			{
				var token = cancelAllTask.Token;
				await taskAttack;
				taskAttack = UniTask.CompletedTask;
				if (token.IsCancellationRequested) return;
			}

			cancelAllTask.Cancel();
			cancelAllTask.Dispose();
			cancelAllTask = new CancellationTokenSource();
			animator.enabled = false;
			spriteRenderer.sprite = fall;

			throw new NotImplementedException();
		}


		[SerializeField] private float swimpSpeed;
		private async UniTask Swimp(Vector2 direction)
		{
#if DEBUG
			if (direction.x == 0 || direction.y != 0) throw new InvalidOperationException();
#endif
			animator.enabled = true;
			await transform.Move_deprecated(transform.position + (Vector3)direction, swimpSpeed, cancelAllTask.Token);
		}


		private async UniTask MoveToY(float y)
		{
			animator.enabled = false;
			bool down = y == lowY;
			spriteRenderer.sprite = down ? fall : jump;
			var dest = transform.position;
			dest.y = y;
			var token = cancelAllTask.Token;
			await transform.Move_deprecated(dest, down ? downSpeed : upSpeed, token);
			if (token.IsCancellationRequested) return;
			if (down) animator.enabled = true;
		}
	}
}