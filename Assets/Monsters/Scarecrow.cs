using Cysharp.Threading.Tasks;
using System;
using UnityEngine;


namespace NinjaSchool.Monsters
{
	public sealed class Scarecrow : Monster
	{
		[SerializeField] private SpriteRenderer spriteRenderer;
		[SerializeField] private Sprite idle, leftDamage, rightDamage;


		protected override bool isRightFace => throw new NotSupportedException();

		protected override Vector2 direction
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		protected override UniTask Attack() => throw new NotSupportedException();


		protected override async UniTask Damage(bool right)
		{
			spriteRenderer.sprite = right ? rightDamage : leftDamage;
			if (await UniTask.Delay(300, cancellationToken: cancelAllTask.Token).SuppressCancellationThrow()) return;
			spriteRenderer.sprite = idle;
		}
	}
}