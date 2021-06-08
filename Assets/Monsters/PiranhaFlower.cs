using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
using System.Threading;

namespace NinjaSchool.Monsters
{
	public sealed class PiranhaFlower : Monster
	{
		[SerializeField] private Animator animator;
		[SerializeField] private SpriteRenderer spriteRenderer;
		[SerializeField] private Sprite attack, damage;


		protected override Vector2 direction
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}



		protected override async UniTask Attack()
		{
			animator.enabled = false;
			spriteRenderer.sprite = attack;
			if (await UniTask.Delay(300, cancellationToken: cancelAllTask.Token).SuppressCancellationThrow()) return;
			animator.enabled = true;

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
			animator.enabled = false;
			spriteRenderer.sprite = damage;


			throw new System.NotImplementedException();
		}
	}
}