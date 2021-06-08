using Cysharp.Threading.Tasks;
using UnityEngine;


namespace NinjaSchool.Monsters
{
	public sealed class X : Monster
	{
		protected override Vector2 direction { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }


		protected override async UniTask Attack()
		{
			throw new System.NotImplementedException();
		}


		protected override async UniTask Damage(bool right)
		{
			throw new System.NotImplementedException();
		}
	}
}