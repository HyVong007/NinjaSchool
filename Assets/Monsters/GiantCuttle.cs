using Cysharp.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;


namespace NinjaSchool.Monsters
{
	public sealed class GiantCuttle : Monster
	{
		[SerializeField] private GameObject normal, damage, attack;


		protected override Vector2 direction { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void DisableAll()
		{
			normal.SetActive(false);
			damage.SetActive(false);
			attack.SetActive(false);
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
			damage.SetActive(true);

			throw new System.NotImplementedException();

			// Nếu còn sống và chưa hủy
			damage.SetActive(false);
			normal.SetActive(true);
		}


		[SerializeField] private float runSpeed;
		private async UniTask Run(Vector2 direction)
		{
#if DEBUG
			if (direction.x == 0 || direction.y != 0) throw new InvalidOperationException();
#endif
			DisableAll();
			isRightFace = direction.x > 0;
			normal.SetActive(true);
			await transform.Move_deprecated(transform.position + (Vector3)direction, runSpeed, cancelAllTask.Token);
		}


		protected override async UniTask Attack()
		{
			DisableAll();
			attack.SetActive(true);
			if (await UniTask.Delay(300, cancellationToken: cancelAllTask.Token).SuppressCancellationThrow()) return;
			attack.SetActive(false);

			throw new NotImplementedException();

			// Nếu chưa hủy
			normal.SetActive(true);
		}
	}
}