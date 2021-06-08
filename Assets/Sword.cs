using RotaryHeart.Lib.SerializableDictionary;
using System;
using UnityEngine;


namespace NinjaSchool
{
	[DisallowMultipleComponent]
	public sealed class Sword : MonoBehaviour
	{
		[Serializable]
		public struct Data
		{
			public GameObject obj;
			public GameObject[] commons, extras;
			public GameObject V2, V3;
		}
		//public SerializableDictionaryBase<Ninja.AttackMode, Data> data;
	}
}