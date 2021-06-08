using Cysharp.Threading.Tasks;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


namespace NinjaSchool.Monsters
{
	public sealed class FlyingBeetle : Monster
	{
		public enum Color
		{
			Red, Black, Green, Yellow
		}


		private Color Δcolor;
		public Color color
		{
			get => Δcolor;

			set
			{
				Δcolor = value;
				if (animator.enabled) animator.SetTrigger(TRIGGERS[value]);
				else spriteRenderer.sprite = taskDamage.Status == UniTaskStatus.Pending ? states[value].damage : states[value].idle;
			}
		}


		private static readonly IReadOnlyDictionary<Color, int> TRIGGERS = new Dictionary<Color, int>
		{
			[Color.Red] = Animator.StringToHash("RED"),
			[Color.Black] = Animator.StringToHash("BLACK"),
			[Color.Green] = Animator.StringToHash("GREEN"),
			[Color.Yellow] = Animator.StringToHash("YELLOW")
		};

		[Serializable]
		private sealed class State
		{
			public Sprite idle, damage;
		}

		[Serializable]
		private sealed class Color_State_Dict : SerializableDictionaryBase<Color, State> { }
		[SerializeField] private Color_State_Dict states;
		[SerializeField] private SpriteRenderer spriteRenderer;
		[SerializeField] private Animator animator;


		protected override Vector2 direction { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }


		protected override async UniTask Attack()
		{
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
			spriteRenderer.sprite = states[color].damage;


			throw new NotImplementedException();
		}


		[Serializable]
		private sealed class Color_Float_Dict : SerializableDictionaryBase<Color, float> { }
		[SerializeField] private Color_Float_Dict flySpeeds;

		private async UniTask Fly(Vector2 direction)
		{
#if DEBUG
			if (direction == default) throw new InvalidOperationException();
#endif
			animator.enabled = true;
			animator.SetTrigger(TRIGGERS[color]);
			await transform.Move_deprecated(transform.position + (Vector3)direction, flySpeeds[color], cancelAllTask.Token);
		}
	}
}