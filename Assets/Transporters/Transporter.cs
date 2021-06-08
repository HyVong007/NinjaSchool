using System.Threading;
using UnityEngine;


namespace NinjaSchool.Transporters
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
	public abstract class Transporter : MonoBehaviour
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
	}
}