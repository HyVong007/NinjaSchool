using Cysharp.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;


namespace NinjaSchool.Monsters
{
	public sealed class Pig : Monster
	{
		[SerializeField] private GameObject idle, run, damage;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void DisableAll()
		{
			idle.SetActive(false);
			run.SetActive(false);
			damage.SetActive(false);
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
			DisableAll();
			isRightFace = right;
			damage.SetActive(true);

			throw new System.NotImplementedException();

			// nếu chưa chết và chưa bị hủy
			damage.SetActive(false);
			idle.SetActive(true);
		}


		protected override Vector2 direction
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Δdirection;

			set
			{
				throw new NotImplementedException();
			}
		}


		[SerializeField] private float runSpeed;
		private async UniTask Run(Vector2 direction)
		{
#if DEBUG
			if (direction.x == 0 || direction.y != 0) throw new InvalidOperationException();
#endif
			DisableAll();
			isRightFace = direction.x > 0;
			run.SetActive(true);
			var token = cancelAllTask.Token;
			await transform.Move_deprecated(transform.position + (Vector3)direction, runSpeed, token);
			if (token.IsCancellationRequested) return;
			run.SetActive(false);
			idle.SetActive(true);
		}


		protected override async UniTask Attack()
		{
			DisableAll();
			run.SetActive(true);
			if (await UniTask.Delay(300, cancellationToken: cancelAllTask.Token).SuppressCancellationThrow()) return;
			run.SetActive(false);

			throw new NotImplementedException();

			idle.SetActive(true);
		}
	}
}