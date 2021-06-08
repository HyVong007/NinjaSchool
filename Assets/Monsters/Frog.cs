using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;


namespace NinjaSchool.Monsters
{
	public sealed class Frog : Monster
	{
		[SerializeField] private SpriteRenderer spriteRenderer;
		[SerializeField] private Sprite idle, jump, damage;


		protected override Vector2 direction { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }


		[SerializeField] private float jumpSpeed;
		private async UniTask Jump(Vector2 direction)
		{
#if DEBUG
			if (direction.x == 0 || direction.y != 0) throw new InvalidOperationException();
#endif
			isRightFace = direction.x > 0;
			spriteRenderer.sprite = jump;
			var token = cancelAllTask.Token;
			await transform.Move_deprecated(transform.position + (Vector3)direction, jumpSpeed, token);
			if (token.IsCancellationRequested) return;
			spriteRenderer.sprite = idle;
		}


		protected override async UniTask Attack()
		{
			spriteRenderer.sprite = jump;
			if (await UniTask.Delay(300, cancellationToken: cancelAllTask.Token).SuppressCancellationThrow()) return;
			spriteRenderer.sprite = idle;

			throw new NotImplementedException();
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
			spriteRenderer.sprite = damage;

			throw new NotImplementedException();

			spriteRenderer.sprite = idle;
		}
	}
}